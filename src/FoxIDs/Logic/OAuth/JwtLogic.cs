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
using UrlCombineLib;

namespace FoxIDs.Logic
{
    public class JwtLogic<TClient, TScope, TClaim> : LogicBase where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly TrackKeyLogic trackKeyLogic;
        private readonly TrackIssuerLogic trackIssuerLogic;
        private readonly ClaimsLogic<TClient, TScope, TClaim> claimsLogic;
        private readonly OAuthResourceScopeLogic<TClient, TScope, TClaim> oauthResourceScopeLogic;

        public JwtLogic(TelemetryScopedLogger logger, TrackKeyLogic trackKeyLogic, TrackIssuerLogic trackIssuerLogic, ClaimsLogic<TClient, TScope, TClaim> claimsLogic, OAuthResourceScopeLogic<TClient, TScope, TClaim> oauthResourceScopeLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.trackKeyLogic = trackKeyLogic;
            this.trackIssuerLogic = trackIssuerLogic;
            this.claimsLogic = claimsLogic;
            this.oauthResourceScopeLogic = oauthResourceScopeLogic;
        }

        public async Task<string> CreateIdTokenAsync(TClient client, IEnumerable<Claim> claims, IEnumerable<string> selectedScopes, string nonce, IEnumerable<string> responseTypes, string code, string accessToken, string algorithm)
        {
            if (!(client is OidcDownClient))
            {
                throw new InvalidOperationException("Include ID Token only possible for OIDC Down Client.");
            }

            var onlyIdToken = !responseTypes.Contains(IdentityConstants.ResponseTypes.Code) && !responseTypes.Contains(IdentityConstants.ResponseTypes.Token);
            var idTokenClaims = new List<Claim>(await claimsLogic.FilterJwtClaimsAsync(client, claims, selectedScopes, includeIdTokenClaims: true, includeAccessTokenClaims: onlyIdToken));

            var clientClaims = claimsLogic.GetClientJwtClaims(client, onlyIdTokenClaims: true);
            if(clientClaims?.Count() > 0)
            {
                idTokenClaims.AddRange(clientClaims);
            }

            if(nonce != null)
            {
                idTokenClaims.AddClaim(JwtClaimTypes.Nonce, nonce);
            }

            if(!onlyIdToken)
            {
                if (responseTypes.Contains(IdentityConstants.ResponseTypes.Token))
                {
                    idTokenClaims.AddClaim(JwtClaimTypes.AtHash, await accessToken.LeftMostBase64urlEncodedHash(algorithm));
                }
                if (responseTypes.Contains(IdentityConstants.ResponseTypes.Code))
                {
                    idTokenClaims.AddClaim(JwtClaimTypes.CHash, await code.LeftMostBase64urlEncodedHash(algorithm));
                }
            }

            var token = JwtHandler.CreateToken(trackKeyLogic.GetPrimarySecurityKey(RouteBinding.Key), trackIssuerLogic.GetIssuer(), client.ClientId, idTokenClaims, expiresIn: (client as OidcDownClient).IdTokenLifetime, algorithm: algorithm);
            return await token.ToJwtString();
        }

        public async Task<string> CreateAccessTokenAsync(TClient client, IEnumerable<Claim> claims, IEnumerable<string> selectedScopes, string algorithm)
        {
            var accessTokenClaims = new List<Claim>();

            (var audiences, var audienceScopes) = await oauthResourceScopeLogic.GetValidResourceAsync(client, selectedScopes);
            if (audiences.Count() > 0)
            {
                accessTokenClaims.AddClaim(JwtClaimTypes.Scope, audienceScopes.ToSpaceList());
            }
            else
            {
                audiences.Add(client.ClientId);
            }

            accessTokenClaims.AddClaim(JwtClaimTypes.ClientId, client.ClientId);

            accessTokenClaims.AddRange(await claimsLogic.FilterJwtClaimsAsync(client, claims, selectedScopes, includeAccessTokenClaims: true));

            var clientClaims = claimsLogic.GetClientJwtClaims(client);
            if (clientClaims?.Count() > 0)
            {
                accessTokenClaims.AddRange(clientClaims);
            }

            var token = JwtHandler.CreateToken(trackKeyLogic.GetPrimarySecurityKey(RouteBinding.Key), trackIssuerLogic.GetIssuer(), audiences, accessTokenClaims, expiresIn: client.AccessTokenLifetime, algorithm: algorithm, typ: IdentityConstants.JwtHeaders.MediaTypes.AtJwt);
            return await token.ToJwtString();
        }

        public async Task<ClaimsPrincipal> ValidatePartyClientTokenAsync(TClient client, string token, bool validateLifetime = true)
        {
            var issuerSigningKeys = new List<JsonWebKey>();
            issuerSigningKeys.Add(RouteBinding.Key.PrimaryKey.Key);
            if(RouteBinding.Key.SecondaryKey != null)
            {
                issuerSigningKeys.Add(RouteBinding.Key.SecondaryKey.Key);
            }

            try
            {
                (var claimsPrincipal, var securityToken) = await Task.FromResult(JwtHandler.ValidateToken(token, trackIssuerLogic.GetIssuer(), issuerSigningKeys, audience: client.ClientId, validateLifetime: validateLifetime));
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
            var issuerSigningKeys = new List<JsonWebKey>();
            issuerSigningKeys.Add(RouteBinding.Key.PrimaryKey.Key);
            if (RouteBinding.Key.SecondaryKey != null)
            {
                issuerSigningKeys.Add(RouteBinding.Key.SecondaryKey.Key);
            }

            try
            {
                (var claimsPrincipal, var securityToken) = await Task.FromResult(JwtHandler.ValidateToken(token, trackIssuerLogic.GetIssuer(), issuerSigningKeys, validateAudience: validateAudience, validateLifetime: validateLifetime));
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
