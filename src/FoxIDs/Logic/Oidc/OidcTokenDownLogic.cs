using System;
using System.Linq;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace FoxIDs.Logic
{
    public class OidcTokenDownLogic<TParty, TClient, TScope, TClaim> : OAuthTokenDownLogic<TParty, TClient, TScope, TClaim> where TParty : OidcDownParty<TClient, TScope, TClaim> where TClient : OidcDownClient<TScope, TClaim> where TScope : OidcDownScope<TClaim> where TClaim : OidcDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly PlanUsageLogic planUsageLogic;
        private readonly OidcJwtDownLogic<TClient, TScope, TClaim> oidcJwtDownLogic;
        private readonly OAuthAuthCodeGrantDownLogic<TClient, TScope, TClaim> oauthAuthCodeGrantDownLogic;
        private readonly OAuthRefreshTokenGrantDownLogic<TClient, TScope, TClaim> oauthRefreshTokenGrantDownLogic;
        private readonly OAuthTokenExchangeDownLogic<TParty, TClient, TScope, TClaim> oauthTokenExchangeDownLogic;

        public OidcTokenDownLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, PlanUsageLogic planUsageLogic, OidcJwtDownLogic<TClient, TScope, TClaim> oidcJwtDownLogic, OAuthAuthCodeGrantDownLogic<TClient, TScope, TClaim> oauthAuthCodeGrantDownLogic, OAuthRefreshTokenGrantDownLogic<TClient, TScope, TClaim> oauthRefreshTokenGrantDownLogic, SecretHashLogic secretHashLogic, ClaimTransformLogic claimTransformLogic, OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic, OAuthTokenExchangeDownLogic<TParty, TClient, TScope, TClaim> oauthTokenExchangeDownLogic, IHttpContextAccessor httpContextAccessor) : base(logger, tenantDataRepository, planUsageLogic, oidcJwtDownLogic, secretHashLogic, claimTransformLogic, oauthResourceScopeDownLogic, oauthTokenExchangeDownLogic, httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.planUsageLogic = planUsageLogic;
            this.oidcJwtDownLogic = oidcJwtDownLogic;
            this.oauthAuthCodeGrantDownLogic = oauthAuthCodeGrantDownLogic;
            this.oauthRefreshTokenGrantDownLogic = oauthRefreshTokenGrantDownLogic;
            this.oauthTokenExchangeDownLogic = oauthTokenExchangeDownLogic;
        }

        public override async Task<IActionResult> TokenRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "AppReg, OIDC Token request.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantDataRepository.GetAsync<TParty>(partyId);
            if (party.Client == null)
            {
                throw new NotSupportedException("Application Client not configured.");
            }
            logger.SetScopeProperty(Constants.Logs.DownPartyClientId, party.Client.ClientId);

            var formDictionary = GetFormDictionary();
            var tokenRequest = formDictionary.ToObject<TokenRequest>();
            if (tokenRequest.GrantType != IdentityConstants.GrantTypes.TokenExchange)
            {
                logger.ScopeTrace(() => $"AppReg, Token request '{tokenRequest.ToJson()}'.", traceType: TraceTypes.Message);
            }

            var codeVerifierSecret = party.Client.RequirePkce ? formDictionary.ToObject<CodeVerifierSecret>() : null;
            if (codeVerifierSecret != null)
            {
                logger.ScopeTrace(() => $"AppReg, Code verifier secret (PKCE) '{new CodeVerifierSecret { CodeVerifier = $"{(codeVerifierSecret.CodeVerifier?.Length > 10 ? codeVerifierSecret.CodeVerifier.Substring(0, 3) : string.Empty)}..." }.ToJson()}'.", traceType: TraceTypes.Message);
            }

            try
            {
                logger.SetScopeProperty(Constants.Logs.GrantType, tokenRequest.GrantType);
                switch (tokenRequest.GrantType)
                {
                    case IdentityConstants.GrantTypes.AuthorizationCode:
                        var validatePkce = party.Client.RequirePkce && codeVerifierSecret != null;
                        ValidateAuthCodeRequest(party.Client, tokenRequest);
                        await ValidateClientAuthenticationAsync(party, tokenRequest, HttpContext.Request.Headers, formDictionary, clientAuthenticationRequired: !validatePkce);
                        planUsageLogic.LogTokenRequestEvent(UsageLogTokenTypes.AuthorizationCode);
                        return await AuthorizationCodeGrantAsync(party, tokenRequest, validatePkce, codeVerifierSecret);
                    case IdentityConstants.GrantTypes.RefreshToken:
                        ValidateRefreshTokenRequest(party.Client, tokenRequest);
                        await ValidateClientAuthenticationAsync(party, tokenRequest, HttpContext.Request.Headers, formDictionary, clientAuthenticationRequired: !party.Client.RequirePkce);
                        planUsageLogic.LogTokenRequestEvent(UsageLogTokenTypes.RefreshToken);
                        return await RefreshTokenGrantAsync(party, tokenRequest);
                    case IdentityConstants.GrantTypes.ClientCredentials:
                        ValidateClientCredentialsRequest(party.Client, tokenRequest);
                        await ValidateClientAuthenticationAsync(party, tokenRequest, HttpContext.Request.Headers, formDictionary);
                        planUsageLogic.LogTokenRequestEvent(UsageLogTokenTypes.ClientCredentials);
                        return await ClientCredentialsGrantAsync(party, tokenRequest);
                    case IdentityConstants.GrantTypes.TokenExchange:
                        var tokenExchangeRequest = formDictionary.ToObject<TokenExchangeRequest>();
                        logger.ScopeTrace(() => $"AppReg, Token exchange request '{tokenExchangeRequest.ToJson()}'.", traceType: TraceTypes.Message);
                        oauthTokenExchangeDownLogic.ValidateTokenExchangeRequest(party.Client, tokenExchangeRequest);
                        await ValidateClientAuthenticationAsync(party, tokenRequest, HttpContext.Request.Headers, formDictionary);
                        planUsageLogic.LogTokenRequestEvent(UsageLogTokenTypes.TokenExchange);
                        return await oauthTokenExchangeDownLogic.TokenExchangeAsync(party, tokenExchangeRequest);

                    default:
                        throw new OAuthRequestException($"Unsupported grant type '{tokenRequest.GrantType}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.UnsupportedGrantType };
                }
            }
            catch (ArgumentException ex)
            {
                throw new OAuthRequestException($"{ex.Message}{(ex is ArgumentNullException ? " is null or empty." : string.Empty)}", ex) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
            }
        }

        private Dictionary<string, string> GetFormDictionary()
        {
            try
            {
                return HttpContext.Request.Form.ToDictionary();
            }
            catch (Exception ex)
            {
                throw new Exception("Token request do not contain a form request.", ex);
            }
        }

        protected override async Task<IActionResult> AuthorizationCodeGrantAsync(TParty party, TokenRequest tokenRequest, bool validatePkce, CodeVerifierSecret codeVerifierSecret)
        {
            var authCodeGrant = await oauthAuthCodeGrantDownLogic.GetAndValidateAuthCodeGrantAsync(tokenRequest.Code, tokenRequest.RedirectUri, party.Client.ClientId);
            if (validatePkce)
            {
                await ValidatePkceAsync(party.Client, authCodeGrant.CodeChallenge, authCodeGrant.CodeChallengeMethod, codeVerifierSecret);
            }
            logger.ScopeTrace(() => "AppReg, OIDC Authorization code grant accepted.", triggerEvent: true);

            try
            {
                var tokenResponse = new TokenResponse
                {
                    TokenType = IdentityConstants.TokenTypes.Bearer,
                    ExpiresIn = party.Client.AccessTokenLifetime,
                };

                string algorithm = IdentityConstants.Algorithms.Asymmetric.RS256;
                var claims = authCodeGrant.Claims.ToClaimList();
                logger.SetUserScopeProperty(claims);
                var scopes = authCodeGrant.Scope.ToSpaceList();

                tokenResponse.AccessToken = await oidcJwtDownLogic.CreateAccessTokenAsync(party.Client, party.UsePartyIssuer ? RouteBinding.RouteUrl : null, claims, scopes, algorithm);
                var responseTypes = new[] { IdentityConstants.ResponseTypes.IdToken, IdentityConstants.ResponseTypes.Token };
                tokenResponse.IdToken = await oidcJwtDownLogic.CreateIdTokenAsync(party.Client, party.UsePartyIssuer ? RouteBinding.RouteUrl : null, claims, scopes, authCodeGrant.Nonce, responseTypes, null, tokenResponse.AccessToken, algorithm);

                if (scopes.Contains(IdentityConstants.DefaultOidcScopes.OfflineAccess))
                {
                    (var refreshTokenGrant, var refreshToken) = await oauthRefreshTokenGrantDownLogic.CreateRefreshTokenGrantAsync(party.Client, claims, authCodeGrant.Scope);
                    tokenResponse.RefreshToken = refreshToken;
                    SetRefreshTokenExpiresIn(refreshTokenGrant, tokenResponse);
                }

                logger.ScopeTrace(() => $"Token response '{tokenResponse.ToJson()}'.", traceType: TraceTypes.Message);
                logger.ScopeTrace(() => "AppReg, OIDC Token response.", triggerEvent: true);
                return new JsonResult(tokenResponse);
            }
            catch (KeyException kex)
            {
                throw new OAuthRequestException(kex.Message, kex) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.ServerError };
            }
        }

        protected override async Task<IActionResult> RefreshTokenGrantAsync(TParty party, TokenRequest tokenRequest)
        {
            (var refreshTokenGrant, var newRefreshToken) = await oauthRefreshTokenGrantDownLogic.ValidateAndUpdateRefreshTokenGrantAsync(party.Client, tokenRequest.RefreshToken);
            logger.ScopeTrace(() => "AppReg, OIDC Refresh Token grant accepted.", triggerEvent: true);

            var scopes = refreshTokenGrant.Scope.ToSpaceList();
            if (!tokenRequest.Scope.IsNullOrEmpty())
            {
                var requestScopes = tokenRequest.Scope.ToSpaceList();
                var invalidScope = requestScopes.Where(s => !scopes.Contains(s) && IdentityConstants.DefaultOidcScopes.OpenId != s);
                if (invalidScope.Count() > 0)
                {
                    throw new OAuthRequestException($"Invalid scope '{tokenRequest.Scope}' not originally granted.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidScope };
                }

                scopes = scopes.Where(s => requestScopes.Contains(s)).Select(s => s).ToArray();
            }
            try
            {
                var tokenResponse = new TokenResponse
                {
                    TokenType = IdentityConstants.TokenTypes.Bearer,
                    ExpiresIn = party.Client.AccessTokenLifetime,
                };

                string algorithm = IdentityConstants.Algorithms.Asymmetric.RS256;
                var claims = refreshTokenGrant.Claims.ToClaimList();
                logger.SetUserScopeProperty(claims);

                tokenResponse.AccessToken = await oidcJwtDownLogic.CreateAccessTokenAsync(party.Client, party.UsePartyIssuer ? RouteBinding.RouteUrl : null, claims, scopes, algorithm);
                var responseTypes = new[] { IdentityConstants.ResponseTypes.IdToken, IdentityConstants.ResponseTypes.Token };
                tokenResponse.IdToken = await oidcJwtDownLogic.CreateIdTokenAsync(party.Client, party.UsePartyIssuer ? RouteBinding.RouteUrl : null, claims, scopes, null, responseTypes, null, tokenResponse.AccessToken, algorithm);

                if (!newRefreshToken.IsNullOrEmpty())
                {
                    tokenResponse.RefreshToken = newRefreshToken;
                    SetRefreshTokenExpiresIn(refreshTokenGrant, tokenResponse);
                }

                logger.ScopeTrace(() => $"Token response '{tokenResponse.ToJson()}'.", traceType: TraceTypes.Message);
                logger.ScopeTrace(() => "AppReg, OIDC Token response.", triggerEvent: true);
                return new JsonResult(tokenResponse);
            }
            catch (KeyException kex)
            {
                throw new OAuthRequestException(kex.Message, kex) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.ServerError };
            }
        }

        private static void SetRefreshTokenExpiresIn(RefreshTokenGrant refreshTokenGrant, TokenResponse tokenResponse)
        {
            if (refreshTokenGrant is RefreshTokenTtlGrant refreshTokenTtlGrant)
            {
                var grantExpireAt = DateTimeOffset.FromUnixTimeSeconds(refreshTokenTtlGrant.CreateTime).AddSeconds(refreshTokenTtlGrant.TimeToLive);
                var utcNow = DateTimeOffset.UtcNow;
                if (grantExpireAt > utcNow)
                {
                    tokenResponse.RefreshTokenExpiresIn = Convert.ToInt64((grantExpireAt - utcNow).TotalSeconds);
                }
            }
        }
    }
}
