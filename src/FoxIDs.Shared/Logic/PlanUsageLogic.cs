using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

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
            logger.Event($"Usage {UsageLogTypes.Login}.{partyType} event.", properties: new Dictionary<string, string> { { Constants.Logs.UsageType, UsageLogTypes.Login.ToString() }, { Constants.Logs.UsageLoginType, partyType.ToString() } });
        }

        public void LogTokenRequestEvent(UsageLogTokenTypes tokenType)
        {
            logger.Event($"Usage {UsageLogTypes.TokenRequest}.{tokenType} event.", properties: new Dictionary<string, string> { { Constants.Logs.UsageType, UsageLogTypes.TokenRequest.ToString() }, { Constants.Logs.UsageTokenType, tokenType.ToString() } });
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
    }
}
