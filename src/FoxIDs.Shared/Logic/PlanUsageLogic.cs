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
using FoxIDs.Repository;

namespace FoxIDs.Logic
{
    public class PlanUsageLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ICacheProvider cacheProvider;
        private readonly PlanCacheLogic planCacheLogic;
        private readonly IMasterDataRepository masterDataRepository;

        public PlanUsageLogic(TelemetryScopedLogger logger, ICacheProvider cacheProvider, PlanCacheLogic planCacheLogic, IMasterDataRepository masterDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.cacheProvider = cacheProvider;
            this.planCacheLogic = planCacheLogic;
            this.masterDataRepository = masterDataRepository;
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

        public async Task LogPasswordlessSmsEventAsync(string phone)
        {
            await LogSmsEventAsync(UsageLogTypes.Passwordless, phone);
        }
        public void LogPasswordlessEmailEvent()
        {
            LogEmailEvent(UsageLogTypes.Passwordless);
        }

        public async Task LogConfirmationSmsEventAsync(string phone)
        {
            await LogSmsEventAsync(UsageLogTypes.Confirmation, phone);
        }
        public void LogConfirmationEmailEvent()
        {
            LogEmailEvent(UsageLogTypes.Confirmation);
        }

        public async Task LogSetPasswordSmsEventAsync(string phone)
        {
            await LogSmsEventAsync(UsageLogTypes.SetPassword, phone);
        }
        public void LogSetPasswordEmailEvent()
        {
            LogEmailEvent(UsageLogTypes.SetPassword);
        }

        public void LogMfaAuthAppEvent()
        {
            LogEvent(UsageLogTypes.Mfa);
        }
        public async Task LogMfaSmsEventAsync(string phone)
        {
            await LogSmsEventAsync(UsageLogTypes.Mfa, phone);
        }
        public void LogMfaEmailEvent()
        {
            LogEmailEvent(UsageLogTypes.Mfa);
        }

        public async Task LogSmsEventAsync(UsageLogTypes planUsageType, string phone)
        {
            var properties = new Dictionary<string, string> { { Constants.Logs.UsageSms, "1" } };
            if (RouteBinding.SendSms == null)
            {
                var smsPrices = await LoadAndCreateSmsPrices();
                decimal price = 0.0M;
                if (smsPrices.Countries?.Count() > 0)
                {
                    var phoneNumber = phone.TrimStart('+');
                    var queryprice = smsPrices.Countries?.Where(c => phoneNumber.StartsWith(c.PhoneCode.ToString())).OrderByDescending(c => c.PhoneCode.ToString().Length).Select(c => c.Price).FirstOrDefault();
                    if (!queryprice.HasValue || !(queryprice > 0))
                    {
                        throw new Exception($"Phone number '{phone}' country code not supported.");
                    }
                    price = queryprice.Value;
                }

                properties.Add(Constants.Logs.UsageSmsPrice, price.ToString(CultureInfo.InvariantCulture));
            }

            LogEvent(planUsageType, UsageLogSendTypes.Sms, properties);
        }

        public void LogEmailEvent(UsageLogTypes planUsageType)
        {
            var properties = new Dictionary<string, string> { { Constants.Logs.UsageEmail, "1" } };
            LogEvent(planUsageType, UsageLogSendTypes.Email, properties);
        }

        public void LogEvent(UsageLogTypes planUsageType, UsageLogSendTypes? sendTypes, Dictionary<string, string> properties)
        {
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
                        rating += 0.03;
                    }
                    if (scopedLogger.LogClaimTrace)
                    {
                        rating += 0.07;
                    }
                    if (scopedLogger.LogMessageTrace)
                    {
                        rating += 0.1;
                    }
                }

                var scopedStreamLoggers = RouteBinding.Logging?.ScopedStreamLoggers;
                if (scopedStreamLoggers?.Count() > 0)
                {
                    foreach (var scopedStreamLogger in scopedStreamLoggers)
                    {
                        rating += 0.01;

                        if (scopedStreamLogger.LogInfoTrace)
                        {
                            rating += 0.001;
                        }
                        if (scopedStreamLogger.LogClaimTrace)
                        {
                            rating += 0.002;
                        }
                        if (scopedStreamLogger.LogMessageTrace)
                        {
                            rating += 0.004;
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

        private async Task<SmsPrices> LoadAndCreateSmsPrices()
        {
            var mSmsPrices = await masterDataRepository.GetAsync<SmsPrices>(await SmsPrices.IdFormatAsync(), required: false);
            if (mSmsPrices == null)
            {
                mSmsPrices = new SmsPrices
                {
                    Id = await SmsPrices.IdFormatAsync()
                };
                await masterDataRepository.CreateAsync(mSmsPrices);
            }

            return mSmsPrices;
        }
    }
}
