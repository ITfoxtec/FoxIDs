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
            logger.Event($"Usage {UsageLogTypes.Login}.{partyType} event.", properties: new Dictionary<string, string> { { Constants.Logs.UsageType, UsageLogTypes.Login.ToString() }, { Constants.Logs.UsageLoginType, partyType.ToString() }, { Constants.Logs.UsageAddRating, addRating.ToString(CultureInfo.InvariantCulture) } });
        }

        public void LogTokenRequestEvent(UsageLogTokenTypes tokenType)
        {
            var addRating = GetLogAddRating(tokenType);
            logger.Event($"Usage {UsageLogTypes.TokenRequest}.{tokenType} event.", properties: new Dictionary<string, string> { { Constants.Logs.UsageType, UsageLogTypes.TokenRequest.ToString() }, { Constants.Logs.UsageTokenType, tokenType.ToString() }, { Constants.Logs.UsageAddRating, addRating.ToString(CultureInfo.InvariantCulture) } });
        }

        public void LogControlApiGetEvent()
        {
            LogEvent(UsageLogTypes.ControlApiGet);
        }

        public void LogControlApiUpdateEvent()
        {
            LogEvent(UsageLogTypes.ControlApiUpdate);
        }

        private void LogEvent(UsageLogTypes planUsageType)
        {
            logger.Event($"Usage {planUsageType} event.", properties: new Dictionary<string, string> { { Constants.Logs.UsageType, planUsageType.ToString() } });
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
