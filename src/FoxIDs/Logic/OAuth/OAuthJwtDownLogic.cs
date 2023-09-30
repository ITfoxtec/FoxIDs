using ITfoxtec.Identity;
using ITfoxtec.Identity.Models;
using ITfoxtec.Identity.Tokens;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OAuthJwtDownLogic<TClient, TScope, TClaim> : LogicSequenceBase where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly TrackKeyLogic trackKeyLogic;
        private readonly TrackIssuerLogic trackIssuerLogic;
        private readonly ClaimsOAuthDownLogic<TClient, TScope, TClaim> claimsOAuthDownLogic;
        private readonly OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic;

        public OAuthJwtDownLogic(TelemetryScopedLogger logger, TrackKeyLogic trackKeyLogic, TrackIssuerLogic trackIssuerLogic, ClaimsOAuthDownLogic<TClient, TScope, TClaim> claimsOAuthDownLogic, OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.trackKeyLogic = trackKeyLogic;
            this.trackIssuerLogic = trackIssuerLogic;
            this.claimsOAuthDownLogic = claimsOAuthDownLogic;
            this.oauthResourceScopeDownLogic = oauthResourceScopeDownLogic;
        }

        public async Task<string> CreateAccessTokenAsync(TClient client, IEnumerable<Claim> claims, IEnumerable<string> selectedScopes, string algorithm)
        {
            var accessTokenClaims = new List<Claim>();

            (var audiences, var audienceScopes) = await oauthResourceScopeDownLogic.GetValidResourceAsync(client, selectedScopes);

            accessTokenClaims.AddRange(await claimsOAuthDownLogic.FilterJwtClaimsAsync(client, claims, selectedScopes, includeAccessTokenClaims: true));

            var clientClaims = claimsOAuthDownLogic.GetClientJwtClaims(client);
            if (clientClaims?.Count() > 0)
            {
                accessTokenClaims.AddRange(clientClaims);
            }

            if (audiences.Count() > 0 && audienceScopes.Count() > 0)
            {
                accessTokenClaims.AddClaim(JwtClaimTypes.Scope, audienceScopes.ToSpaceList());
            }
            else
            {
                audiences.Add(client.ClientId);
            }

            accessTokenClaims.AddClaim(JwtClaimTypes.ClientId, client.ClientId);

            logger.ScopeTrace(() => $"Down, JWT access token claims '{accessTokenClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            var token = JwtHandler.CreateToken(await trackKeyLogic.GetPrimarySecurityKeyAsync(RouteBinding.Key), trackIssuerLogic.GetIssuer(), audiences, accessTokenClaims, expiresIn: client.AccessTokenLifetime, algorithm: algorithm, typ: IdentityConstants.JwtHeaders.MediaTypes.AtJwt);
            return await token.ToJwtString();
        }

        public async Task<ClaimsPrincipal> ValidatePartyClientTokenAsync(TClient client, string token, bool validateLifetime = true)
        {
            var issuerSigningKeys = new List<JsonWebKey>
            {
                RouteBinding.Key.PrimaryKey.Key
            };
            if (RouteBinding.Key.SecondaryKey != null)
            {
                issuerSigningKeys.Add(RouteBinding.Key.SecondaryKey.Key);
            }

            try
            {
                (var claimsPrincipal, _) = await Task.FromResult(JwtHandler.ValidateToken(token, trackIssuerLogic.GetIssuer(), issuerSigningKeys, audience: client.ClientId, validateLifetime: validateLifetime));
                return claimsPrincipal;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Party client JWT not valid. Client id '{client.ClientId}', Route '{RouteBinding.Route}'.");
                return null;
            }
        }

        public async Task<ClaimsPrincipal> ValidateTokenAsync(string token, string audience = null, bool validateLifetime = true)
        {
            var issuerSigningKeys = new List<JsonWebKey>
            {
                RouteBinding.Key.PrimaryKey.Key
            };
            if (RouteBinding.Key.SecondaryKey != null)
            {
                issuerSigningKeys.Add(RouteBinding.Key.SecondaryKey.Key);
            }

            try
            {
                (var claimsPrincipal, _) = await Task.FromResult(JwtHandler.ValidateToken(token, trackIssuerLogic.GetIssuer(), issuerSigningKeys, audience: audience, validateAudience: !audience.IsNullOrWhiteSpace(), validateLifetime: validateLifetime));
                return claimsPrincipal;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"JWT not valid. Route '{RouteBinding.Route}'.");
                return null;
            }
        }

        public async Task<ClaimsPrincipal> ValidateClientAssertionAsync(string clientAssertion, string issuer, List<JsonWebKey> clientKeys, string audience)
        {
            (var claimsPrincipal, _) = await Task.FromResult(JwtHandler.ValidateToken(clientAssertion, issuer, clientKeys, audience));
            return claimsPrincipal;

        }
    }
}
