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
    public class OAuthRefreshTokenGrantDownLogic<TClient, TScope, TClaim> : OAuthRefreshTokenGrantDownBaseLogic where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly ClaimsOAuthDownLogic<TClient, TScope, TClaim> claimsOAuthDownLogic;

        public OAuthRefreshTokenGrantDownLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, ClaimsOAuthDownLogic<TClient, TScope, TClaim> claimsOAuthDownLogic, IHttpContextAccessor httpContextAccessor) : base(logger, tenantDataRepository, httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.claimsOAuthDownLogic = claimsOAuthDownLogic;
        }

        public async Task<string> CreateRefreshTokenGrantAsync(TClient client, List<Claim> claims, string scope)
        {
            logger.ScopeTrace(() => $"Create Refresh Token grant, Route '{RouteBinding.Route}'.");

            CheckeConfiguration(client);

            var grantClaims = await claimsOAuthDownLogic.FilterJwtClaimsAsync(client, claims, scope?.ToSpaceList(), includeIdTokenClaims: true, includeAccessTokenClaims: true);

            var refreshToken = CreateRefreshToken(client);
            _ = await CreateGrantInternal(client, grantClaims.ToClaimAndValues(), scope, refreshToken);

            logger.ScopeTrace(() => $"Refresh token grant created, Refresh Token '{refreshToken}'.");
            return refreshToken;
        }

        public async Task<(RefreshTokenGrant, string)> ValidateAndUpdateRefreshTokenGrantAsync(TClient client, string refreshToken)
        {
            logger.ScopeTrace(() => $"Get, validate and update Refresh Token grant, Route '{RouteBinding.Route}', Refresh Token '{refreshToken}'.");

            CheckeConfiguration(client);

            var grant = await GetRefreshTokenGrantAsync(client, refreshToken);
            if (grant == null)
            {
                throw new OAuthRefreshTokenGrantNotFoundException($"Refresh Token grant not found for client id '{client.ClientId}' and probably timed out.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
            }
            if (!grant.ClientId.Equals(client.ClientId, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new OAuthRequestException($"Refresh Token grant not found for client id '{client.ClientId}', invalid client id.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
            }

            var utcNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (grant.CreateTime + client.RefreshTokenAbsoluteLifetime <= utcNow)
            {
                throw new OAuthRequestException("Refresh Token grant has surpassed absolute lifetime.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
            }
            logger.ScopeTrace(() => $"Refresh Token grant valid, Refresh Token '{refreshToken}'.");

            string newRefreshToken = null;
            if (client.RefreshTokenUseOneTime == true)
            {
                newRefreshToken = refreshToken = CreateRefreshToken(client);
            }
            grant = await CreateGrantInternal(client, grant.Claims, grant.Scope, refreshToken, grant.CreateTime, utcNow);

            logger.ScopeTrace(() => $"Refresh Token grant updated, Refresh Token '{refreshToken}'.");
            return (grant, newRefreshToken);
        }

        public async Task DeleteRefreshTokenGrantsBySessionIdAsync(string sessionId)
        {
            if (sessionId.IsNullOrWhiteSpace()) return;

            logger.ScopeTrace(() => $"Delete Refresh Token grants, Route '{RouteBinding.Route}', Session ID '{sessionId}'.");

            var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
            var ttlGrantCount = await tenantDataRepository.DeleteListAsync<RefreshTokenTtlGrant>(idKey, d => d.DataType.Equals(Constants.Models.DataType.RefreshTokenGrant) && d.SessionId == sessionId);
            if (ttlGrantCount > 0)
            {
                logger.ScopeTrace(() => $"TTL Refresh Token grants deleted, Session ID '{sessionId}'.");
            }
            var grantCount = await tenantDataRepository.DeleteListAsync<RefreshTokenGrant>(idKey, d => d.DataType.Equals(Constants.Models.DataType.RefreshTokenGrant) && d.SessionId == sessionId);
            if (grantCount > 0)
            {
                logger.ScopeTrace(() => $"Refresh Token grants deleted, Session ID '{sessionId}'.");
            }
        }

        public async Task DeleteRefreshTokenGrantsByPhoneAsync(string phone)
        {
            if (phone.IsNullOrWhiteSpace()) return;

            logger.ScopeTrace(() => $"Delete Refresh Token grants, Route '{RouteBinding.Route}', Phone '{phone}'.");

            var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
            var ttlGrantCount = await tenantDataRepository.DeleteListAsync<RefreshTokenTtlGrant>(idKey, d => d.DataType.Equals(Constants.Models.DataType.RefreshTokenGrant) && d.Phone == phone);
            if (ttlGrantCount > 0)
            {
                logger.ScopeTrace(() => $"TTL Refresh Token grants deleted, Phone '{phone}'.");
            }
            var grantCount = await tenantDataRepository.DeleteListAsync<RefreshTokenGrant>(idKey, d => d.DataType.Equals(Constants.Models.DataType.RefreshTokenGrant) && d.Phone == phone);
            if (grantCount > 0)
            {
                logger.ScopeTrace(() => $"Refresh Token grants deleted, Phone '{phone}'.");
            }
        }

        public async Task DeleteRefreshTokenGrantsByEmailAsync(string email)
        {
            if (email.IsNullOrWhiteSpace()) return;

            logger.ScopeTrace(() => $"Delete Refresh Token grants, Route '{RouteBinding.Route}', Email '{email}'.");

            var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
            var ttlGrantCount = await tenantDataRepository.DeleteListAsync<RefreshTokenTtlGrant>(idKey, d => d.DataType.Equals(Constants.Models.DataType.RefreshTokenGrant) && d.Email == email);
            if (ttlGrantCount > 0)
            {
                logger.ScopeTrace(() => $"TTL Refresh Token grants deleted, Email '{email}'.");
            }
            var grantCount = await tenantDataRepository.DeleteListAsync<RefreshTokenGrant>(idKey, d => d.DataType.Equals(Constants.Models.DataType.RefreshTokenGrant) && d.Email == email);
            if (grantCount > 0)
            {
                logger.ScopeTrace(() => $"Refresh Token grants deleted, Email '{email}'.");
            }
        }

        //public async Task DeleteRefreshTokenGrantsByUserNameAsync(string username)
        //{
        //    if (username.IsNullOrWhiteSpace()) return;

        //    logger.ScopeTrace(() => $"Delete Refresh Token grants, Route '{RouteBinding.Route}', Username '{username}'.");

        //    var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
        //    var ttlGrantCount = await tenantDataRepository.DeleteListAsync<RefreshTokenTtlGrant>(idKey, d => d.DataType.Equals(Constants.Models.DataType.RefreshTokenGrant) && d.Username == username);
        //    if (ttlGrantCount > 0)
        //    {
        //        logger.ScopeTrace(() => $"TTL Refresh Token grants deleted, Username '{username}'.");
        //    }
        //    var grantCount = await tenantDataRepository.DeleteListAsync<RefreshTokenGrant>(idKey, d => d.DataType.Equals(Constants.Models.DataType.RefreshTokenGrant) && d.Username == username);
        //    if (grantCount > 0)
        //    {
        //        logger.ScopeTrace(() => $"Refresh Token grants deleted, Username '{username}'.");
        //    }
        //}

        private void CheckeConfiguration(TClient client)
        {
            if (!client.RefreshTokenLifetime.HasValue)
                throw new EndpointException("Client RefreshTokenLifetime not configured.") { RouteBinding = RouteBinding };

            if (!client.RefreshTokenAbsoluteLifetime.HasValue)
                throw new EndpointException("Client RefreshTokenAbsoluteLifetime not configured.") { RouteBinding = RouteBinding };
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

            var id = await RefreshTokenGrant.IdFormatAsync(grantIdKey);
            if (refreshToken.StartsWith('u'))
            {
                return await tenantDataRepository.GetAsync<RefreshTokenGrant>(id, required: false, delete: client.RefreshTokenUseOneTime == true);
            }
            else if (refreshToken.StartsWith('t'))
            {
                return await tenantDataRepository.GetAsync<RefreshTokenTtlGrant>(id, required: false, delete: client.RefreshTokenUseOneTime == true);
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

            grant.Sub = GetTruncatedClaimValue(claims, JwtClaimTypes.Subject);
            grant.Email = GetTruncatedClaimValue(claims, JwtClaimTypes.Email);
            grant.Phone = GetTruncatedClaimValue(claims, JwtClaimTypes.PhoneNumber);
            grant.Username = GetTruncatedClaimValue(claims, JwtClaimTypes.PreferredUsername);

            await grant.SetIdAsync(new RefreshTokenGrant.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, RefreshToken = refreshToken });
            await tenantDataRepository.SaveAsync(grant);

            return grant;
        }

        private string GetTruncatedClaimValue(List<ClaimAndValues> claims, string claimType)
        {
            var value = claims.Where(c => c.Claim == claimType).Select(c => c.Values.FirstOrDefault()).FirstOrDefault();
            if (value?.Length > Constants.Models.Claim.ValueLength)
            {
                value = value.Substring(0, Constants.Models.Claim.ValueLength);
                try
                {
                    throw new Exception($"The refresh token grant '{claimType}' is truncated '{value}', maximum length of '{Constants.Models.Claim.ValueLength}'.");
                }
                catch (Exception ex)
                {
                    logger.Warning(ex);
                }
            }
            return value;
        }
    }
}