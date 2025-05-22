using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

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

        public async Task<Used> DoCalculationAsync(DateOnly datePointer, Tenant tenant, CancellationToken stoppingToken)
        {
            var id = await Used.IdFormatAsync(new Used.IdKey { TenantName = tenant.Name, PeriodYear = datePointer.Year, PeriodMonth = datePointer.Month });
            var used = await tenantDataRepository.GetAsync<Used>(id, required: false);
            if (used?.IsUsageCalculated == true)
            {
                return used;
            }

            logger.Event($"Usage, calculation for tenant '{tenant.Name}' started.");

            if (used == null)
            {
                used = new Used
                {
                    Id = id,
                    TenantName = tenant.Name
                };

                used.PeriodBeginDate = new DateOnlySerializable(datePointer.Year, datePointer.Month, 1);
                used.PeriodEndDate = used.PeriodBeginDate.AddMonths(1).AddDays(-1);
                if (tenant.CreateTime.HasValue && tenant.CreateTime.Value > 0)
                {
                    var tenantCreateDate = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(tenant.CreateTime.Value).LocalDateTime);
                    used.PeriodBeginDate = (used.PeriodBeginDate.ToDateOnly() < tenantCreateDate && tenantCreateDate < used.PeriodEndDate.ToDateOnly() ? tenantCreateDate : used.PeriodBeginDate.ToDateOnly()).ToDateOnlySerializable();
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
                    IncludeAdditional = true,
                    IncludeControlApi = true
                }, tenant.Name, null, isMasterTrack: true);

            used.IsUsageCalculated = true;
            used.Tracks = usageDbLogs.Items.Where(i => i.Type == Api.UsageLogTypes.Track).Select(i => i.Value).FirstOrDefault();
            used.Users = usageDbLogs.Items.Where(i => i.Type == Api.UsageLogTypes.User).Select(i => i.Value).FirstOrDefault();
            
            used.Logins = usageLogs.Items.Where(i => i.Type == Api.UsageLogTypes.Login).Select(i => i.Value).FirstOrDefault();
            used.TokenRequests = usageLogs.Items.Where(i => i.Type == Api.UsageLogTypes.TokenRequest).Select(i => i.Value).FirstOrDefault();

            used.Sms = GetSubItemsSum(usageLogs.Items, Api.UsageLogTypes.Sms);
            if (used.Sms > 0)
            {
                used.SmsPrice = GetSubItemsSmsPriceSum(usageLogs.Items);
            }
            used.Emails = GetSubItemsSum(usageLogs.Items, Api.UsageLogTypes.Email);

            used.ControlApiGets = usageLogs.Items.Where(i => i.Type == Api.UsageLogTypes.ControlApiGet).Select(i => i.Value).FirstOrDefault();
            used.ControlApiUpdates = usageLogs.Items.Where(i => i.Type == Api.UsageLogTypes.ControlApiUpdate).Select(i => i.Value).FirstOrDefault();

            stoppingToken.ThrowIfCancellationRequested();
            await tenantDataRepository.SaveAsync(used);

            logger.Event($"Usage, calculation for tenant '{tenant.Name}' done.");
            return used;
        }

        private decimal GetSubItemsSmsPriceSum(IEnumerable<Api.UsageLogItem> items)
        {
            decimal smsTotal = 0;
            decimal smsPriceTotal = 0;
            var outherItems = items.Where(i => i.SubItems?.Any(si => si.Type == Api.UsageLogTypes.Sms) == true)?.ToList();
            if (outherItems?.Count() > 0)
            {
                foreach (var outherItem in outherItems)
                {
                    var smsItem = outherItem.SubItems?.Where(si => si.Type == Api.UsageLogTypes.Sms)?.FirstOrDefault();
                    if (smsItem != null && smsItem.Value > 0)
                    {
                        smsTotal += smsItem.Value;
                        var smsPrice = smsItem.SubItems?.Where(si => si.Type == Api.UsageLogTypes.SmsPrice).Select(si => si.Value)?.Sum();
                        if (smsPrice.HasValue)
                        {
                            smsPriceTotal += smsPrice.Value * smsItem.Value;
                        }
                    }
                }
            }

            if (smsTotal > 0)
            {
               return Math.Round(smsPriceTotal / smsTotal, 4); 
            }
            else
            {
                return 0;
            }
        }

        private decimal GetSubItemsSum(IEnumerable<Api.UsageLogItem> items, Api.UsageLogTypes usageLogTypes)
        {
            var value = items.Select(i => i.SubItems?.Where(si => si.Type == usageLogTypes).Select(si => si.Value)?.Sum())?.Sum();
            return value.HasValue? Math.Round(value.Value, 0) : 0;
        }
    }
}
