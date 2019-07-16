using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OAuthRefreshTokenGrantLogic<TClient, TScope, TClaim> : LogicBase where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantRepository;
        private readonly ClaimsLogic<TClient, TScope, TClaim> claimsLogic;

        public OAuthRefreshTokenGrantLogic(TelemetryScopedLogger logger, ITenantRepository tenantRepository, ClaimsLogic<TClient, TScope, TClaim> claimsLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.claimsLogic = claimsLogic;
        }

        public async Task<string> CreateRefreshTokenGrantAsync(TClient client, List<Claim> claims, string scope)
        {
            logger.ScopeTrace($"Create Refresh Token grant, Route '{RouteBinding.Route}'.");

            CheckeConfiguration(client);

            var grantClaims = await claimsLogic.FilterJwtClaims(client, claims, scope?.ToSpaceList(), includeIdTokenClaims: true, includeAccessTokenClaims: true);

            var refreshToken = CreateRefreshToken(client);
            await CreateGrantInternal(client, grantClaims.ToClaimAndValues(), scope, refreshToken);

            logger.ScopeTrace($"Refresh token grant created, Refresh Token '{refreshToken}'.");
            return refreshToken;
        }

        public async Task<(RefreshTokenGrant, string)> ValidateAndUpdateRefreshTokenGrantAsync(TClient client, string refreshToken)
        {
            logger.ScopeTrace($"Get, validate and update Refresh Token grant, Route '{RouteBinding.Route}', Refresh Token '{refreshToken}'.");

            CheckeConfiguration(client);

            var grant = await GetRefreshTokenGrantAsync(client, refreshToken);
            if (grant == null || !grant.ClientId.Equals(client.ClientId, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new OAuthRequestException($"Refresh Token grant not found for client id '{client.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
            }

            var utcNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (grant.CreateTime + client.RefreshTokenAbsoluteLifetime <= utcNow)
            {
                throw new OAuthRequestException("Refresh Token grant has surpassed absolute lifetime.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
            }
            logger.ScopeTrace($"Refresh Token grant valid, Refresh Token '{refreshToken}'.");

            string newRefreshToken = null;
            if (client.RefreshTokenUseOneTime == true)
            {
                newRefreshToken = refreshToken = CreateRefreshToken(client);
            }
            grant = await CreateGrantInternal(client, grant.Claims, grant.Scope, refreshToken, grant.CreateTime, utcNow);

            logger.ScopeTrace($"Refresh Token grant updated, Refresh Token '{refreshToken}'.");
            return (grant, newRefreshToken);
        }

        public async Task DeleteRefreshTokenGrantAsync(TClient client, string sessionId)
        {
            if (sessionId.IsNullOrWhiteSpace()) return;

            logger.ScopeTrace($"Delete Refresh Token grant, Route '{RouteBinding.Route}', Session ID '{sessionId}'.");

            var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
            await idKey.ValidateObjectAsync();
            RefreshTokenGrant grant = await tenantRepository.DeleteAsync<RefreshTokenTtlGrant>(idKey, d => d.SessionId == sessionId);
            if (grant != null)
            {
                logger.ScopeTrace($"TTL Refresh Token grant deleted, Refresh Token '{grant.RefreshToken}', Session ID '{sessionId}'.");
            }
            else
            {
                grant = await tenantRepository.DeleteAsync<RefreshTokenGrant>(idKey, d => d.SessionId == sessionId);
                if (grant != null)
                {
                    logger.ScopeTrace($"Refresh Token grant deleted, Refresh Token '{grant.RefreshToken}', Session ID '{sessionId}'.");
                }
            }
        }

        private void CheckeConfiguration(TClient client)
        {
            if (!client.RefreshTokenLifetime.HasValue)
                throw new EndpointException("Client RefreshTokenLifetime not configured.") { RouteBinding = RouteBinding };

            if (!client.RefreshTokenAbsoluteLifetime.HasValue)
                throw new EndpointException("Client RefreshTokenAbsoluteLifetime not configured.") { RouteBinding = RouteBinding };

            if (!client.RefreshTokenUseOneTime.HasValue)
                throw new EndpointException("Client RefreshTokenUseOneTime not configured.") { RouteBinding = RouteBinding };

            if (!client.RefreshTokenLifetimeUnlimited.HasValue)
                throw new EndpointException("Client RefreshTokenLifetimeUnlimited not configured.") { RouteBinding = RouteBinding };
        }

        private string CreateRefreshToken(TClient client)
        {
            var refreshToken = RandomGenerator.Generate(64);
            if (client.RefreshTokenLifetimeUnlimited == true)
            {
                return $"u{refreshToken}";
            }
            else
            {
                return $"t{refreshToken}";
            }
        }

        private async Task<RefreshTokenGrant> GetRefreshTokenGrantAsync(TClient client, string refreshToken)
        {
            var grantIdKey = new RefreshTokenGrant.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, RefreshToken = refreshToken };
            await grantIdKey.ValidateObjectAsync();

            var id = RefreshTokenGrant.IdFormat(grantIdKey);
            if (refreshToken.StartsWith('u'))
            {
                return await tenantRepository.GetAsync<RefreshTokenGrant>(id, requered: false, delete: client.RefreshTokenUseOneTime == true);
            }
            else if (refreshToken.StartsWith('t'))
            {
                return await tenantRepository.GetAsync<RefreshTokenTtlGrant>(id, requered: false, delete: client.RefreshTokenUseOneTime == true);
            }
            else
            {
                throw new OAuthRequestException("Invalid first info char in Refresh Token grant.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
            }
        }

        private async Task<RefreshTokenGrant> CreateGrantInternal(TClient client, List<ClaimAndValues> claims, string scope, string refreshToken, long? createTime = null, long? utcNow = null)
        {
            RefreshTokenGrant grant = null;
            if (refreshToken.StartsWith('u'))
            {
                grant = new RefreshTokenGrant();
            }
            else if (refreshToken.StartsWith('t'))
            {
                var refreshTokenAbsoluteLifetime = client.RefreshTokenAbsoluteLifetime.Value;
                if (createTime.HasValue && utcNow.HasValue)
                {
                    refreshTokenAbsoluteLifetime = refreshTokenAbsoluteLifetime - Convert.ToInt32(utcNow.Value - createTime.Value);
                }
                grant = new RefreshTokenTtlGrant { TimeToLive = client.RefreshTokenLifetime.Value <= refreshTokenAbsoluteLifetime ? client.RefreshTokenLifetime.Value : refreshTokenAbsoluteLifetime };
            }
            else
            {
                throw new OAuthRequestException("Invalid first info char in Refresh Token grant.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
            }

            grant.CreateTime = createTime ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            grant.Claims = claims;
            grant.ClientId = client.ClientId;
            grant.Scope = scope;
            grant.SessionId = claims.Where(c => c.Claim == JwtClaimTypes.SessionId).Select(c => c.Values.FirstOrDefault()).FirstOrDefault();

            await grant.SetIdAsync(new RefreshTokenGrant.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, RefreshToken = refreshToken });
            await tenantRepository.SaveAsync(grant);

            return grant;
        }
    }
}