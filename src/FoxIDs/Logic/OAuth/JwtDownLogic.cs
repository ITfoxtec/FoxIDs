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
    public class JwtDownLogic<TClient, TScope, TClaim> : LogicSequenceBase where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly TrackKeyLogic trackKeyLogic;
        private readonly TrackIssuerLogic trackIssuerLogic;
        private readonly ClaimsOAuthDownLogic<TClient, TScope, TClaim> claimsOAuthDownLogic;
        private readonly OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic;

        public JwtDownLogic(TelemetryScopedLogger logger, TrackKeyLogic trackKeyLogic, TrackIssuerLogic trackIssuerLogic, ClaimsOAuthDownLogic<TClient, TScope, TClaim> claimsOAuthDownLogic, OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.trackKeyLogic = trackKeyLogic;
            this.trackIssuerLogic = trackIssuerLogic;
            this.claimsOAuthDownLogic = claimsOAuthDownLogic;
            this.oauthResourceScopeDownLogic = oauthResourceScopeDownLogic;
        }

        public async Task<string> CreateIdTokenAsync(TClient client, IEnumerable<Claim> claims, IEnumerable<string> selectedScopes, string nonce, IEnumerable<string> responseTypes, string code, string accessToken, string algorithm)
        {
            if (!(client is OidcDownClient))
            {
                throw new InvalidOperationException("Include ID Token only possible for OIDC Down Client.");
            }

            var onlyIdToken = !responseTypes.Contains(IdentityConstants.ResponseTypes.Code) && !responseTypes.Contains(IdentityConstants.ResponseTypes.Token);
            var idTokenClaims = new List<Claim>(await claimsOAuthDownLogic.FilterJwtClaimsAsync(client, claims, selectedScopes, includeIdTokenClaims: true, includeAccessTokenClaims: onlyIdToken));

            var clientClaims = claimsOAuthDownLogic.GetClientJwtClaims(client, onlyIdTokenClaims: true);
            if(clientClaims?.Count() > 0)
            {
                idTokenClaims.AddRange(clientClaims);
            }

            if(!nonce.IsNullOrEmpty())
            {
                idTokenClaims.AddClaim(JwtClaimTypes.Nonce, nonce);
            }

            if(!onlyIdToken)
            {
                if (responseTypes.Contains(IdentityConstants.ResponseTypes.Token))
                {
                    idTokenClaims.AddClaim(JwtClaimTypes.AtHash, await accessToken.LeftMostBase64urlEncodedHashAsync(algorithm));
                }
                if (responseTypes.Contains(IdentityConstants.ResponseTypes.Code))
                {
                    idTokenClaims.AddClaim(JwtClaimTypes.CHash, await code.LeftMostBase64urlEncodedHashAsync(algorithm));
                }
            }

            logger.ScopeTrace(() => $"Down, JWT ID token claims '{idTokenClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            var token = JwtHandler.CreateToken(await trackKeyLogic.GetPrimarySecurityKeyAsync(RouteBinding.Key), trackIssuerLogic.GetIssuer(), client.ClientId, idTokenClaims, expiresIn: (client as OidcDownClient).IdTokenLifetime, algorithm: algorithm);
            return await token.ToJwtString();
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

        public async Task<ClaimsPrincipal> ValidateTokenAsync(string token, bool validateAudience = false, bool validateLifetime = true)
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
                (var claimsPrincipal, _) = await Task.FromResult(JwtHandler.ValidateToken(token, trackIssuerLogic.GetIssuer(), issuerSigningKeys, validateAudience: validateAudience, validateLifetime: validateLifetime));
                return claimsPrincipal;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"JWT not valid. Route '{RouteBinding.Route}'.");
                return null;
            }
        }
    }
}
