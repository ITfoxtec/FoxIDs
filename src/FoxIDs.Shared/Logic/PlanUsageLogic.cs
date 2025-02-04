using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace FoxIDs.Logic
{
    public class PlanUsageLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;

        public PlanUsageLogic(TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
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
                properties = new Dictionary<string, string> { { Constants.Logs.Email, "1" } };
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
    }
}
