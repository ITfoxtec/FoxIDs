﻿using ITfoxtec.Identity;
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
using Microsoft.IdentityModel.Tokens;

namespace FoxIDs.Logic
{
    public class JwtLogic<TClient, TScope, TClaim> : LogicBase where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly TrackKeyLogic trackKeyLogic;
        private readonly ClaimsLogic<TClient, TScope, TClaim> claimsLogic;
        private readonly OAuthResourceScopeLogic<TClient, TScope, TClaim> oauthResourceScopeLogic;

        public JwtLogic(TelemetryScopedLogger logger, TrackKeyLogic trackKeyLogic, ClaimsLogic<TClient, TScope, TClaim> claimsLogic, OAuthResourceScopeLogic<TClient, TScope, TClaim> oauthResourceScopeLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.trackKeyLogic = trackKeyLogic;
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
            var idTokenClaims = new List<Claim>(await claimsLogic.FilterJwtClaims(client, claims, selectedScopes, includeIdTokenClaims: true, includeAccessTokenClaims: onlyIdToken));

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

            var token = JwtHandler.CreateToken(trackKeyLogic.GetSecurityKey(RouteBinding.PrimaryKey), Issuer(RouteBinding), client.ClientId, idTokenClaims, expiresIn: (client as OidcDownClient).IdTokenLifetime, algorithm: algorithm, x509CertificateSHA1Thumbprint: RouteBinding.PrimaryKey.Key.Kid);
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

            accessTokenClaims.AddRange(await claimsLogic.FilterJwtClaims(client, claims, selectedScopes, includeAccessTokenClaims: true));

            var token = JwtHandler.CreateToken(trackKeyLogic.GetSecurityKey(RouteBinding.PrimaryKey), Issuer(RouteBinding), audiences, accessTokenClaims, expiresIn: client.AccessTokenLifetime, algorithm: algorithm, x509CertificateSHA1Thumbprint: RouteBinding.PrimaryKey.Key.Kid);
            return await token.ToJwtString();
        }

        public async Task<ClaimsPrincipal> ValidatePartyClientTokenAsync(TClient client, string token, bool validateLifetime = true)
        {
            var issuerSigningKeys = new List<JsonWebKey>();
            issuerSigningKeys.Add(RouteBinding.PrimaryKey.Key);
            if(RouteBinding.SecondaryKey != null)
            {
                issuerSigningKeys.Add(RouteBinding.SecondaryKey.Key);
            }

            try
            {
                (var claimsPrincipal, var securityToken) = await Task.FromResult(JwtHandler.ValidateToken(token, Issuer(RouteBinding), issuerSigningKeys, audience: client.ClientId, validateLifetime: validateLifetime));
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
            issuerSigningKeys.Add(RouteBinding.PrimaryKey.Key);
            if (RouteBinding.SecondaryKey != null)
            {
                issuerSigningKeys.Add(RouteBinding.SecondaryKey.Key);
            }

            try
            {
                (var claimsPrincipal, var securityToken) = await Task.FromResult(JwtHandler.ValidateToken(token, Issuer(RouteBinding), issuerSigningKeys, validateAudience: validateAudience, validateLifetime: validateLifetime));
                return claimsPrincipal;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"JWT not valid. Route '{RouteBinding.Route}'.");
                return null;
            }
        }

        private string Issuer(RouteBinding RouteBinding)
        {
            return UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName);
        }
    }
}
