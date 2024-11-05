using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace FoxIDs.Logic.Usage
{
    public class UsageCalculatorLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly UsageLogLogic usageLogLogic;

        public UsageCalculatorLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, UsageLogLogic usageLogLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.usageLogLogic = usageLogLogic;
        }

        public async Task<(bool taskDone, Used used)> DoCalculationAsync(DateOnly datePointer, Tenant tenant, CancellationToken stoppingToken)
        {
            try
            {
                var id = await Used.IdFormatAsync(new Used.IdKey { TenantName = tenant.Name, PeriodYear = datePointer.Year, PeriodMonth = datePointer.Month });
                var used = await tenantDataRepository.GetAsync<Used>(id, required: false);
                if (used?.IsUsageCalculated == true)
                {
                    return (true, used);
                }

                logger.Event($"Usage calculation tenant '{tenant.Name}' started.");

                if (used == null)
                {
                    used = new Used
                    {
                        Id = id,
                        TenantName = tenant.Name
                    };

                    used.PeriodBeginDate = new DateOnly(datePointer.Year, datePointer.Month, 1);
                    used.PeriodEndDate = used.PeriodBeginDate.AddMonths(1).AddDays(-1);
                    if (tenant.CreateTime > 0)
                    {
                        var tenantCreateDate = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(tenant.CreateTime).LocalDateTime);
                        used.PeriodBeginDate = used.PeriodBeginDate < tenantCreateDate && tenantCreateDate < used.PeriodEndDate ? tenantCreateDate : used.PeriodBeginDate;
                    }
                }

                stoppingToken.ThrowIfCancellationRequested();
                var usageDbLogs = await usageLogLogic.GetTrackUsageLogAsync(
                    new Api.UsageLogRequest
                    {
                        OnlyDbQuery = true,
                        TimeScope = Api.UsageLogTimeScopes.ThisMonth,
                        SummarizeLevel = Api.UsageLogSummarizeLevels.Month,
                        IncludeTracks = true,
                        IncludeUsers = true,
                    }, tenant.Name, null, isMasterTrack: true);

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
                    }, tenant.Name, null, isMasterTrack: true);

                used.IsUsageCalculated = true;
                used.Tracks = usageDbLogs.Items.Where(i => i.Type == Api.UsageLogTypes.Track).Select(i => i.Value).FirstOrDefault();
                used.Users = usageDbLogs.Items.Where(i => i.Type == Api.UsageLogTypes.User).Select(i => i.Value).FirstOrDefault();
                used.Logins = usageLogs.Items.Where(i => i.Type == Api.UsageLogTypes.Login).Select(i => i.Value).FirstOrDefault();
                used.TokenRequests = usageLogs.Items.Where(i => i.Type == Api.UsageLogTypes.TokenRequest).Select(i => i.Value).FirstOrDefault();
                used.ControlApiGets = usageLogs.Items.Where(i => i.Type == Api.UsageLogTypes.ControlApiGet).Select(i => i.Value).FirstOrDefault();
                used.ControlApiUpdates = usageLogs.Items.Where(i => i.Type == Api.UsageLogTypes.ControlApiUpdate).Select(i => i.Value).FirstOrDefault();

                stoppingToken.ThrowIfCancellationRequested();
                await tenantDataRepository.SaveAsync(used);

                logger.Event($"Usage calculation tenant '{tenant.Name}' done.");
                return (true, used);
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
                logger.Error(ex, $"Error occurred during tenant '{tenant.Name}' usage calculation.");
                return (false, null);
            }
        }
    }
}
