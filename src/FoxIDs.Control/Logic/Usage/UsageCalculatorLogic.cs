using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FoxIDs.Models.Usage;
using System.Linq;
using FoxIDs.Logic.Caches.Providers;

namespace FoxIDs.Logic.Usage
{
    public class UsageCalculatorLogic : LogicBase
    {
        private readonly TelemetryLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ICacheProvider cacheProvider;
        private readonly ITenantDataRepository tenantDataRepository;

        public UsageCalculatorLogic(TelemetryLogger logger, IServiceProvider serviceProvider, ICacheProvider cacheProvider, ITenantDataRepository tenantDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.cacheProvider = cacheProvider;
            this.tenantDataRepository = tenantDataRepository;
        }

        public async Task<bool> ShouldStartAsync(DateTimeOffset datePointer, CancellationToken stoppingToken)
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
                        await Task.Delay(1000 * 60 * 5, stoppingToken); // 5 minutes

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
                logger.Error(ex, "Error occurred during should start calculation check.");
            }

            return false;
        }

        public async Task DoCalculationAsync(DateTimeOffset datePointer, CancellationToken stoppingToken)
        {
            try
            {
                string paginationToken = null;
                while (!stoppingToken.IsCancellationRequested)
                {
                    logger.Event("Start usage calculation.");

                    (var tenants, paginationToken) = await tenantDataRepository.GetListAsync<Tenant>(whereQuery: t => !string.IsNullOrEmpty(t.PlanName) && t.PlanName != "free" , pageSize: 100, paginationToken: paginationToken);
                    foreach(var tenant in tenants)
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        await DoTenantCalculationAsync(datePointer, tenant.Name, stoppingToken);
                    }

                    if (paginationToken == null)
                    {
                        break;
                    }
                }
                logger.Event("Done calculating usage.");
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
                logger.Error(ex, "Error occurred during usage calculation.");
            }
        }

        private async Task DoTenantCalculationAsync(DateTimeOffset datePointer, string tenantName, CancellationToken stoppingToken)
        {
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                var scopedLogger = scope.ServiceProvider.GetRequiredService<TelemetryScopedLogger>();
                try
                {
                    scopedLogger.Event($"Start tenant '{tenantName}' usage calculation.", properties: new Dictionary<string, string> { { Constants.Logs.TenantName, tenantName } });
                        
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

                    var used = new Used
                    {
                        Id = await Used.IdFormatAsync(new Used.IdKey { TenantName = tenantName, Year = datePointer.Year, Month = datePointer.Month }),
                        Year = datePointer.Year,
                        Month = datePointer.Month,
                        Tracks = usageDbLogs.Items.Where(i => i.Type == Api.UsageLogTypes.Track).Select(i => i.Value).FirstOrDefault(),
                        Users = usageDbLogs.Items.Where(i => i.Type == Api.UsageLogTypes.User).Select(i => i.Value).FirstOrDefault(),
                        Logins = usageLogs.Items.Where(i => i.Type == Api.UsageLogTypes.Login).Select(i => i.Value).FirstOrDefault(),
                        TokenRequests = usageLogs.Items.Where(i => i.Type == Api.UsageLogTypes.TokenRequest).Select(i => i.Value).FirstOrDefault(),
                        ControlApiGets = usageLogs.Items.Where(i => i.Type == Api.UsageLogTypes.ControlApiGet).Select(i => i.Value).FirstOrDefault(),
                        ControlApiUpdates = usageLogs.Items.Where(i => i.Type == Api.UsageLogTypes.ControlApiUpdate).Select(i => i.Value).FirstOrDefault(),
                    };

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
