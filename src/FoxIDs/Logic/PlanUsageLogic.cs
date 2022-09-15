using FoxIDs.Infrastructure;
using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace FoxIDs.Logic
{
    public class PlanUsageLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;

        public PlanUsageLogic(TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
        }

        public void LogActiveUserEvent(IEnumerable<Claim> claims)
        {
            string userId = claims.FindFirstValue(c => c.Type == JwtClaimTypes.Subject);
            if (userId.IsNullOrWhiteSpace())
            {
                try
                {
                    throw new Exception($"'{JwtClaimTypes.Subject}' claim is empty. Active users are counter on each login instead of per month.");
                }
                catch (Exception ex)
                {
                    logger.Warning(ex);
                }

                userId = $"unknown-{Guid.NewGuid()}";
            }

            logger.Event($"Active user.", properties: new Dictionary<string, string> { { Constants.Logs.UsageType, PlanUsageTypes.ActiveUser.ToString() }, { Constants.Logs.ActiveUserId, userId } });
        }
    }
}
