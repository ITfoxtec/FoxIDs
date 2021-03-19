using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SessionUpPartyLogic : LogicBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IDataProtectionProvider dataProtectionProvider;
        private readonly IDistributedCache distributedCache;

        public SessionUpPartyLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, IDataProtectionProvider dataProtectionProvider, IDistributedCache distributedCache, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.dataProtectionProvider = dataProtectionProvider;
            this.distributedCache = distributedCache;
        }

        public async Task CreateSessionAsync<T>(T upParty, List<Claim> claims, long validTo, List<string> authMethods, string sessionId, string externalSessionId) where T : UpParty
        {
            var userId = claims.FindFirstValue(c => c.Type == JwtClaimTypes.Subject);
            logger.ScopeTrace($"Create session up-party for User id '{userId}', User email '{claims.FindFirstValue(c => c.Type == JwtClaimTypes.Email)}', Session id '{sessionId}', External Session id '{externalSessionId}', Route '{RouteBinding.Route}'.");

            var session = new SessionUpParty
            {
                UserId = userId,
                Claims = claims,
                ExternalSessionId = externalSessionId,
                SessionId = sessionId
            };
            session.LastUpdated = session.CreateTime;
            session.AuthMethods = authMethods;

            var absoluteExpiration = DateTimeOffset.FromUnixTimeSeconds(validTo);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = absoluteExpiration
            };
            await distributedCache.SetStringAsync(DataKey(upParty, session), session.ToJson(), options);
            logger.ScopeTrace($"Session up-party created, Session id '{sessionId}', External Session id '{externalSessionId}'.", new Dictionary<string, string> { { "sessionId", session.SessionId }, { "externalSessionId", externalSessionId } });
        }

        private string DataKey<T>(T upParty, SessionUpParty sessionUpParty) where T : UpParty
        {
            var routeBinding = HttpContext.GetRouteBinding();
            return $"{routeBinding.TenantName}.{routeBinding.TrackName}.sesupparty.{typeof(T).Name}.{upParty.Name.ToLower()}.{sessionUpParty.SessionId}.{sessionUpParty.CreateTime}";
        }
    }
}
