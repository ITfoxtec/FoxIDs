using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class AccessTokenSessionLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;

        public AccessTokenSessionLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
        }

        public async Task SaveSessionAsync(IEnumerable<Claim> claims)
        {
            var sessionId = GetSessionId(claims);
            if (sessionId.IsNullOrWhiteSpace())
            {
                return;
            }

            logger.ScopeTrace(() => $"Save access token session, Route '{RouteBinding.Route}', Session ID '{sessionId}'.");

            var session = new AccessTokenSessionTtl
            {
                TimeToLive = AccessTokenSessionTtl.DefaultTimeToLive,
                SessionId = sessionId
            };
            await session.SetIdAsync(new AccessTokenSessionTtl.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, SessionIdHash = await sessionId.HashIdStringAsync() });

            await tenantDataRepository.SaveAsync(session);
        }

        public async Task ValidateSessionAsync(IEnumerable<Claim> claims)
        {
            var sessionId = GetSessionId(claims);
            if (sessionId.IsNullOrWhiteSpace())
            {
                return;
            }

            try
            {
                var id = await AccessTokenSessionTtl.IdFormatAsync(new AccessTokenSessionTtl.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, SessionIdHash = await sessionId.HashIdStringAsync() });
                var session = await tenantDataRepository.GetAsync<AccessTokenSessionTtl>(id, required: false);
                if (session == null)
                {
                    throw new Exception("Access token session does not exist.");
                }
            }
            catch (Exception ex)
            {
                throw new SessionException("Session in JWT not valid", ex);
            }
        }

        public async Task DeleteSessionAsync(string sessionId)
        {
            if (sessionId.IsNullOrWhiteSpace())
            {
                return;
            }

            var id = await AccessTokenSessionTtl.IdFormatAsync(new AccessTokenSessionTtl.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, SessionIdHash = await sessionId.HashIdStringAsync() });
            var session = await tenantDataRepository.GetAsync<AccessTokenSessionTtl>(id, required: false, delete: true);
            if (session != null)
            {
                logger.ScopeTrace(() => $"Access token session deleted, Route '{RouteBinding.Route}', Session ID '{sessionId}'.");
            }
        }

        private static string GetSessionId(IEnumerable<Claim> claims)
        {
            return claims?.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.SessionId);
        }
    }
}
