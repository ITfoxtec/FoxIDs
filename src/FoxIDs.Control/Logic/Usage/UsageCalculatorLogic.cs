using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using FoxIDs.Logic.Caches.Providers;

namespace FoxIDs.Logic.Usage
{
    public class UsageCalculatorLogic : LogicBase
    {
        private readonly ICacheProvider cacheProvider;
        private readonly ITenantDataRepository tenantDataRepository;

        public UsageCalculatorLogic(ICacheProvider cacheProvider, ITenantDataRepository tenantDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.cacheProvider = cacheProvider;
            this.tenantDataRepository = tenantDataRepository;
        }

        public async Task<bool> ShouldStartAsync(TelemetryScopedLogger scopedLogger, DateTimeOffset datePointer, CancellationToken stoppingToken)
        {
            try
            {
                if (!await cacheProvider.ExistsAsync(UsageMonthKey(datePointer)))
                {
                    if (!await cacheProvider.ExistsAsync(UsageMonthCalculateKey(datePointer)))
                    {
                        var myId = Guid.NewGuid().ToString();
                        await cacheProvider.SetAsync(UsageMonthCalculateKey(datePointer), myId, 60 * 60 * 24); // 24 hours lifetime

                        // wait, for others to override
                        await Task.Delay(1000 * 10, stoppingToken); // 10 seconds

                        var keyId = await cacheProvider.GetAsync(UsageMonthCalculateKey(datePointer));
                        if(myId == keyId)
                        {
                            await cacheProvider.SetFlagAsync(UsageMonthKey(datePointer), 60 * 60 * 24 * 62); // min. 2 month lifetime
                            return true;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                scopedLogger.Error(ex, "Error occurred during should start calculation check.");
            }

            return false;
        }

        public async Task DoCalculationAsync(IServiceScope scope, TelemetryScopedLogger scopedLogger, DateTimeOffset datePointer, CancellationToken stoppingToken)
        {
            try
            {
                string paginationToken = null;
                while (!stoppingToken.IsCancellationRequested)
                {
                    scopedLogger.Event("Start usage calculation.");

                    (var tenants, paginationToken) = await tenantDataRepository.GetListAsync<Tenant>(whereQuery: t => !string.IsNullOrEmpty(t.PlanName) && t.PlanName != "free" , pageSize: 100, paginationToken: paginationToken);
                    foreach(var tenant in tenants)
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        await DoTenantCalculationAsync(scope, scopedLogger, datePointer, tenant.Name, stoppingToken);
                    }

                    if (paginationToken == null)
                    {
                        break;
                    }
                }
                scopedLogger.Event("Done calculating usage.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                scopedLogger.Error(ex, "Error occurred during usage calculation.");
            }
        }

        private async Task DoTenantCalculationAsync(IServiceScope scope, TelemetryScopedLogger scopedLogger, DateTimeOffset datePointer, string tenantName, CancellationToken stoppingToken)
        {
            try
            {
                scopedLogger.Event($"Start tenant '{tenantName}' usage calculation.");
                        
                var usageLogLogic = scope.ServiceProvider.GetService<UsageLogLogic>();
                var usageDbLogs = await usageLogLogic.GetTrackUsageLogAsync(
                    new Api.UsageLogRequest
                    {
                        OnlyDbQuery = true,
                        TimeScope = Api.UsageLogTimeScopes.ThisMonth,
                        SummarizeLevel = Api.UsageLogSummarizeLevels.Month,
                        IncludeTracks = true,
                        IncludeUsers = true,
                    }, tenantName, null, isMasterTrack: true);

                stoppingToken.ThrowIfCancellationRequested();

                var usageLogs = await usageLogLogic.GetTrackUsageLogAsync(
                    new Api.UsageLogRequest
                    {
                        TimeScope = Api.UsageLogTimeScopes.LastMonth,
                        SummarizeLevel = Api.UsageLogSummarizeLevels.Month,
                        IncludeLogins = true,
                        IncludeTokenRequests = true,
                        IncludeControlApiGets = true,
                        IncludeControlApiUpdates = true,
                    }, tenantName, null, isMasterTrack: true);

                stoppingToken.ThrowIfCancellationRequested();

                var id = await Used.IdFormatAsync(new Used.IdKey { TenantName = tenantName, PeriodYear = datePointer.Year, PeriodMonth = datePointer.Month });
                var used = await tenantDataRepository.GetAsync<Used>(id, required: false);
                if (used == null)
                {
                    used = new Used
                    {
                        Id = id,
                        TenantName = tenantName,
                        PeriodYear = datePointer.Year,
                        PeriodMonth = datePointer.Month
                    };
                }

                used.Tracks = usageDbLogs.Items.Where(i => i.Type == Api.UsageLogTypes.Track).Select(i => i.Value).FirstOrDefault();
                used.Users = usageDbLogs.Items.Where(i => i.Type == Api.UsageLogTypes.User).Select(i => i.Value).FirstOrDefault();
                used.Logins = usageLogs.Items.Where(i => i.Type == Api.UsageLogTypes.Login).Select(i => i.Value).FirstOrDefault();
                used.TokenRequests = usageLogs.Items.Where(i => i.Type == Api.UsageLogTypes.TokenRequest).Select(i => i.Value).FirstOrDefault();
                used.ControlApiGets = usageLogs.Items.Where(i => i.Type == Api.UsageLogTypes.ControlApiGet).Select(i => i.Value).FirstOrDefault();
                used.ControlApiUpdates = usageLogs.Items.Where(i => i.Type == Api.UsageLogTypes.ControlApiUpdate).Select(i => i.Value).FirstOrDefault();

                await tenantDataRepository.SaveAsync(used);

                scopedLogger.Event($"Done calculating tenant '{tenantName}' usage.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                scopedLogger.Error(ex, $"Error occurred during tenant '{tenantName}' usage calculation.");
            }
        }

        private string UsageMonthKey(DateTimeOffset datePointer)
        {
            return $"usage_month_{SubKey(datePointer)}";
        }
        private string UsageMonthCalculateKey(DateTimeOffset datePointer)
        {
            return $"usage_month_calculate_{SubKey(datePointer)}";
        }
        private string SubKey(DateTimeOffset datePointer)
        {
            return $"{datePointer.Year}-{datePointer.Month}";
        }
    }
}
