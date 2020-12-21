using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OAuthAuthCodeGrantLogic<TClient, TScope, TClaim> : LogicBase where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantRepository;
        private readonly ClaimsLogic<TClient, TScope, TClaim> claimsLogic;

        public OAuthAuthCodeGrantLogic(TelemetryScopedLogger logger, ITenantRepository tenantRepository, ClaimsLogic<TClient, TScope, TClaim> claimsLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.claimsLogic = claimsLogic;
        }

        public async Task<string> CreateAuthCodeGrantAsync(TClient client, List<Claim> claims, string redirectUri, string scope, string nonce, string codeChallenge, string codeChallengeMethod)
        {
            logger.ScopeTrace($"Create Authorization code grant, Route '{RouteBinding.Route}'.");

            if (!client.AuthorizationCodeLifetime.HasValue)
                throw new EndpointException("Client AuthorizationCodeLifetime not configured.") { RouteBinding = RouteBinding };

            var grantClaims = await claimsLogic.FilterJwtClaimsAsync(client, claims, scope?.ToSpaceList(), includeIdTokenClaims: true, includeAccessTokenClaims: true);

            var code = RandomGenerator.Generate(64);
            var grant = new AuthCodeTtlGrant
            {
                TimeToLive = client.AuthorizationCodeLifetime.Value,
                Claims = grantClaims.ToClaimAndValues(),
                ClientId = client.ClientId,
                RedirectUri = redirectUri,
                Scope = scope,
                Nonce = nonce,
                CodeChallenge = codeChallenge,
                CodeChallengeMethod = codeChallengeMethod
            };
            await grant.SetIdAsync(new AuthCodeTtlGrant.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Code = code });
            await tenantRepository.SaveAsync(grant);

            logger.ScopeTrace($"Authorization code grant created, Code '{code}'.");
            return code;
        }

        public async Task<AuthCodeTtlGrant> GetAndValidateAuthCodeGrantAsync(string code, string redirectUri, string clientId)
        {
            logger.ScopeTrace($"Get and validate Authorization code grant, Route '{RouteBinding.Route}', Code '{code}'.");
            
            var grantIdKey = new AuthCodeTtlGrant.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Code = code };
            await grantIdKey.ValidateObjectAsync();

            var grant = await tenantRepository.GetAsync<AuthCodeTtlGrant>(await AuthCodeTtlGrant.IdFormat(grantIdKey), requered: false, delete: true);
            if (grant == null)
            {
                throw new OAuthRequestException("Authorization code grant not found.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
            }

            if (!grant.RedirectUri.Equals(redirectUri, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new OAuthRequestException($"Redirect Uri '{redirectUri}' do not match related grant.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
            }

            if (!grant.ClientId.Equals(clientId, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new OAuthRequestException($"Client id '{clientId}' do not match related grant.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
            }

            logger.ScopeTrace($"Authorization code grant valid, Code '{code}'.");
            return grant;
        }
    }
}