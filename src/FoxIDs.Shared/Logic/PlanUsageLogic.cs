using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using FoxIDs.Logic.Caches.Providers;
using ITfoxtec.Identity;

namespace FoxIDs.Logic
{
    public class PlanUsageLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ICacheProvider cacheProvider;
        private readonly PlanCacheLogic planCacheLogic;

        public PlanUsageLogic(TelemetryScopedLogger logger, ICacheProvider cacheProvider, PlanCacheLogic planCacheLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.cacheProvider = cacheProvider;
            this.planCacheLogic = planCacheLogic;
        }

        public void LogLoginEvent(PartyTypes partyType)
        {
            var addRating = GetLogAddRating();
            var planUsageType = UsageLogTypes.Login;
            LogEvent(planUsageType, message: $"Usage {UsageLogTypes.Login}.{partyType} event.", properties: new Dictionary<string, string> { { Constants.Logs.UsageLoginType, partyType.ToString() }, { Constants.Logs.UsageAddRating, addRating.ToString(CultureInfo.InvariantCulture) } });
        }

        public void LogTokenRequestEvent(UsageLogTokenTypes tokenType)
        {
            var addRating = GetLogAddRating(tokenType);
            var planUsageType = UsageLogTypes.TokenRequest;
            LogEvent(planUsageType, message: $"Usage {planUsageType}.{tokenType} event.", properties: new Dictionary<string, string> { { Constants.Logs.UsageTokenType, tokenType.ToString() }, { Constants.Logs.UsageAddRating, addRating.ToString(CultureInfo.InvariantCulture) } });
        }

        public void LogControlApiGetEvent()
        {
            LogEvent(UsageLogTypes.ControlApiGet);
        }

        public void LogControlApiUpdateEvent()
        {
            LogEvent(UsageLogTypes.ControlApiUpdate);
        }

        public void LogConfirmationEvent(UsageLogSendTypes? sendTypes = null)
        {
            LogEvent(UsageLogTypes.Confirmation, sendTypes);
        }

        public void LogResetPasswordEvent(UsageLogSendTypes? sendTypes = null)
        {
            LogEvent(UsageLogTypes.ResetPassword, sendTypes);
        }

        public void LogMfaEvent(UsageLogSendTypes? sendTypes = null)
        {
            LogEvent(UsageLogTypes.Mfa, sendTypes);
        }

        public void LogEvent(UsageLogTypes planUsageType, UsageLogSendTypes? sendTypes)
        {
            Dictionary<string, string> properties = null;
            if (sendTypes == UsageLogSendTypes.Sms)
            {
                properties = new Dictionary<string, string> { { Constants.Logs.UsageSms, "1" } };
            }
            else if(sendTypes == UsageLogSendTypes.Email)
            {
                properties = new Dictionary<string, string> { { Constants.Logs.UsageEmail, "1" } };
            }
            LogEvent(planUsageType, message: $"Usage {planUsageType}{(sendTypes.HasValue ? $".{sendTypes}" : string.Empty)} event.", properties: properties);
        }

        private void LogEvent(UsageLogTypes planUsageType, string message = null, IDictionary<string, string> properties = null)
        {
            var prop = new Dictionary<string, string> { { Constants.Logs.UsageType, planUsageType.ToString() } };
            if (properties != null)
            {
                foreach(var property in properties)
                {
                    prop.Add(property.Key, property.Value);
                }
            }
            logger.Event(message ?? $"Usage {planUsageType} event.", properties: prop);
        }

        private double GetLogAddRating(UsageLogTokenTypes? tokenType = null)
        {
            var rating = 0.0;
            if (tokenType != UsageLogTokenTypes.UserInfo)
            {
                var scopedLogger = RouteBinding.Logging?.ScopedLogger;
                if (scopedLogger != null)
                {
                    if (scopedLogger.LogInfoTrace)
                    {
                        rating += 0.4;
                    }
                    if (scopedLogger.LogClaimTrace)
                    {
                        rating += 0.8;
                    }
                    if (scopedLogger.LogMessageTrace)
                    {
                        rating += 1.0;
                    }
                    if (scopedLogger.LogMetric)
                    {
                        rating += 0.2;
                    }
                }

                var scopedStreamLoggers = RouteBinding.Logging?.ScopedStreamLoggers;
                if (scopedStreamLoggers?.Count() > 0)
                {
                    foreach (var scopedStreamLogger in scopedStreamLoggers)
                    {
                        rating += 0.2;

                        if (scopedStreamLogger.LogInfoTrace)
                        {
                            rating += 0.02;
                        }
                        if (scopedStreamLogger.LogClaimTrace)
                        {
                            rating += 0.06;
                        }
                        if (scopedStreamLogger.LogMessageTrace)
                        {
                            rating += 0.08;
                        }
                        if (scopedStreamLogger.LogMetric)
                        {
                            rating += 0.01;
                        }
                    }
                }
            }

            return Math.Round(rating, 1);
        }

        public async Task VerifyCanSendSmsAsync()
        {
            if (!RouteBinding.PlanName.IsNullOrEmpty())
            {
                var utcNow = DateTime.UtcNow;
                var plan = await planCacheLogic.GetPlanAsync(RouteBinding.PlanName);

                if (!plan.EnableSms)
                {
                    throw new PlanException(plan, $"SMS two-factor is not supported in the '{plan.Name}' plan.");
                }

                if (plan.Sms.LimitedThreshold > 0)
                {
                    var emailCount = await cacheProvider.GetNumberAsync(SmsSendCountInTenantKey(utcNow));
                    if (emailCount >= plan.Sms.LimitedThreshold)
                    {
                        throw new PlanException(plan, $"Maximum number of SMS ({plan.Sms.LimitedThreshold}) in the '{plan.Name}' plan has been reached.");
                    }
                }
                await cacheProvider.IncrementNumberAsync(SmsSendCountInTenantKey(utcNow), (DateTime.DaysInMonth(utcNow.Year, utcNow.Month) + 1) * 24 * 60 * 60);
            }
        }

        public async Task VerifyCanSendEmailAsync(bool isMfa = false)
        {
            if (!RouteBinding.PlanName.IsNullOrEmpty())
            {
                var utcNow = DateTime.UtcNow;
                var plan = await planCacheLogic.GetPlanAsync(RouteBinding.PlanName);

                if (isMfa && !plan.EnableEmailTwoFactor)
                {
                    throw new PlanException(plan, $"Email two-factor is not supported in the '{plan.Name}' plan.");
                }

                if (plan.Emails.LimitedThreshold > 0)
                {
                    var emailCount = await cacheProvider.GetNumberAsync(EmailsSendCountInTenantKey(utcNow));
                    if (emailCount >= plan.Emails.LimitedThreshold)
                    {
                        throw new PlanException(plan, $"Maximum number of emails ({plan.Emails.LimitedThreshold}) in the '{plan.Name}' plan has been reached.");
                    }
                }
                await cacheProvider.IncrementNumberAsync(EmailsSendCountInTenantKey(utcNow), (DateTime.DaysInMonth(utcNow.Year, utcNow.Month) + 1) * 24 * 60 * 60);
            }
        }

        private string SmsSendCountInTenantKey(DateTime utcNow)
        {
            return $"sms_send_count_{RouteBinding.TenantName}_{utcNow.Month}";
        }
        private string EmailsSendCountInTenantKey(DateTime utcNow)
        {
            return $"emails_send_count_{RouteBinding.TenantName}_{utcNow.Month}";
        }
    }
}
