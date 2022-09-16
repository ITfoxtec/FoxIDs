using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace FoxIDs.Logic
{
    public class PlanUsageLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;

        public PlanUsageLogic(TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
        }

        public void LogLoginEvent()
        {
            logger.Event($"Usage {PlanUsageTypes.Login} event.", properties: new Dictionary<string, string> { { Constants.Logs.UsageType, PlanUsageTypes.Login.ToString() } });
        }

        public void LogTokenRequestEvent(PlanUsageTokenTypes tokenType)
        {
            logger.Event($"Usage {PlanUsageTypes.TokenRequest}.{tokenType} event.", properties: new Dictionary<string, string> { { Constants.Logs.UsageType, PlanUsageTypes.TokenRequest.ToString() }, { Constants.Logs.UsageTokenType, tokenType.ToString() } });
        }
    }
}
