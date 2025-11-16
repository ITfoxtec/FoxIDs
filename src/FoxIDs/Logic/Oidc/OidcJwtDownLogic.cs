using ITfoxtec.Identity;
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
    public class OidcJwtDownLogic<TClient, TScope, TClaim> : OAuthJwtDownLogic<TClient, TScope, TClaim> where TClient : OidcDownClient<TScope, TClaim> where TScope : OidcDownScope<TClaim> where TClaim : OidcDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly TrackKeyLogic trackKeyLogic;
        private readonly TrackIssuerLogic trackIssuerLogic;
        private readonly ClaimsOAuthDownLogic<TClient, TScope, TClaim> claimsOAuthDownLogic;

        public OidcJwtDownLogic(TelemetryScopedLogger logger, TrackKeyLogic trackKeyLogic, TrackIssuerLogic trackIssuerLogic, ClaimsOAuthDownLogic<TClient, TScope, TClaim> claimsOAuthDownLogic, OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic, AccessTokenSessionLogic accessTokenSessionLogic, IHttpContextAccessor httpContextAccessor) : base(logger, trackKeyLogic, trackIssuerLogic, claimsOAuthDownLogic, oauthResourceScopeDownLogic, accessTokenSessionLogic, httpContextAccessor)
        {
            this.logger = logger;
            this.trackKeyLogic = trackKeyLogic;
            this.trackIssuerLogic = trackIssuerLogic;
            this.claimsOAuthDownLogic = claimsOAuthDownLogic;
        }

        public async Task<string> CreateIdTokenAsync(TClient client, string routeUrl, IEnumerable<Claim> claims, IEnumerable<string> selectedScopes, string nonce, IEnumerable<string> responseTypes, string code, string accessToken, string algorithm)
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

            var adjustedClaims = claimsOAuthDownLogic.AdjustClaims(idTokenClaims);
            logger.ScopeTrace(() => $"AppReg, JWT ID token claims '{adjustedClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            var token = JwtHandler.CreateToken(await trackKeyLogic.GetPrimarySecurityKeyAsync(RouteBinding.Key), trackIssuerLogic.GetIssuer(routeUrl), client.ClientId, adjustedClaims, expiresIn: (client as OidcDownClient).IdTokenLifetime, algorithm: algorithm);
            return await token.ToJwtString();
        }
    }
}
