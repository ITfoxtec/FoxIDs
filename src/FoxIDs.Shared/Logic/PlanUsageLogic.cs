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

        public void LogLoginEvent()
        {
            LogEvent(PlanUsageTypes.Login);
        }

        public void LogTokenRequestEvent(PlanUsageTokenTypes tokenType)
        {
            logger.Event($"Usage {PlanUsageTypes.TokenRequest}.{tokenType} event.", properties: new Dictionary<string, string> { { Constants.Logs.UsageType, PlanUsageTypes.TokenRequest.ToString() }, { Constants.Logs.UsageTokenType, tokenType.ToString() } });
        }

        public void LogControlApiGetEvent()
        {
            LogEvent(PlanUsageTypes.ControlApiGet);
        }

        public void LogControlApiUpdateEvent()
        {
            LogEvent(PlanUsageTypes.ControlApiUpdate);
        }

        private void LogEvent(PlanUsageTypes planUsageType)
        {
            logger.Event($"Usage {planUsageType} event.", properties: new Dictionary<string, string> { { Constants.Logs.UsageType, planUsageType.ToString() } });
        }
    }
}
