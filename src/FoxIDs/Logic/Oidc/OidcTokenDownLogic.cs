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

            var formDictionary = HttpContext.Request.Form.ToDictionary();
            var tokenRequest = formDictionary.ToObject<TokenRequest>();
            if (tokenRequest.GrantType != IdentityConstants.GrantTypes.TokenExchange)
            {
                logger.ScopeTrace(() => $"AppReg, Token request '{tokenRequest.ToJsonIndented()}'.", traceType: TraceTypes.Message);
            }

            var codeVerifierSecret = party.Client.RequirePkce ? formDictionary.ToObject<CodeVerifierSecret>() : null;
            if (codeVerifierSecret != null)
            {
                logger.ScopeTrace(() => $"AppReg, Code verifier secret (PKCE) '{new CodeVerifierSecret { CodeVerifier = $"{(codeVerifierSecret.CodeVerifier?.Length > 10 ? codeVerifierSecret.CodeVerifier.Substring(0, 3) : string.Empty)}..." }.ToJsonIndented()}'.", traceType: TraceTypes.Message);
            }

            try
            {
                logger.SetScopeProperty(Constants.Logs.GrantType, tokenRequest.GrantType);
                switch (tokenRequest.GrantType)
                {
                    case IdentityConstants.GrantTypes.AuthorizationCode:
                        var validatePkce = party.Client.RequirePkce && codeVerifierSecret != null;
                        ValidateAuthCodeRequest(party.Client, tokenRequest);
                        await ValidateClientAuthenticationAsync(party.Client, tokenRequest, HttpContext.Request.Headers, formDictionary, clientAuthenticationRequired: !validatePkce);
                        planUsageLogic.LogTokenRequestEvent(UsageLogTokenTypes.AuthorizationCode);
                        return await AuthorizationCodeGrantAsync(party.Client, tokenRequest, validatePkce, codeVerifierSecret);
                    case IdentityConstants.GrantTypes.RefreshToken:
                        ValidateRefreshTokenRequest(party.Client, tokenRequest);
                        await ValidateClientAuthenticationAsync(party.Client, tokenRequest, HttpContext.Request.Headers, formDictionary, clientAuthenticationRequired: !party.Client.RequirePkce);
                        planUsageLogic.LogTokenRequestEvent(UsageLogTokenTypes.RefreshToken);
                        return await RefreshTokenGrantAsync(party.Client, tokenRequest);
                    case IdentityConstants.GrantTypes.ClientCredentials:
                        ValidateClientCredentialsRequest(party.Client, tokenRequest);
                        await ValidateClientAuthenticationAsync(party.Client, tokenRequest, HttpContext.Request.Headers, formDictionary);
                        planUsageLogic.LogTokenRequestEvent(UsageLogTokenTypes.ClientCredentials);
                        return await ClientCredentialsGrantAsync(party, tokenRequest);
                    case IdentityConstants.GrantTypes.TokenExchange:
                        var tokenExchangeRequest = formDictionary.ToObject<TokenExchangeRequest>();
                        logger.ScopeTrace(() => $"AppReg, Token exchange request '{tokenExchangeRequest.ToJsonIndented()}'.", traceType: TraceTypes.Message);
                        oauthTokenExchangeDownLogic.ValidateTokenExchangeRequest(party.Client, tokenExchangeRequest);
                        await ValidateClientAuthenticationAsync(party.Client, tokenRequest, HttpContext.Request.Headers, formDictionary);
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

        protected override async Task<IActionResult> AuthorizationCodeGrantAsync(TClient client, TokenRequest tokenRequest, bool validatePkce, CodeVerifierSecret codeVerifierSecret)
        {
            var authCodeGrant = await oauthAuthCodeGrantDownLogic.GetAndValidateAuthCodeGrantAsync(tokenRequest.Code, tokenRequest.RedirectUri, client.ClientId);
            if (validatePkce)
            {
                await ValidatePkceAsync(client, authCodeGrant.CodeChallenge, authCodeGrant.CodeChallengeMethod, codeVerifierSecret);
            }
            logger.ScopeTrace(() => "AppReg, OIDC Authorization code grant accepted.", triggerEvent: true);

            try
            {
                var tokenResponse = new TokenResponse
                {
                    TokenType = IdentityConstants.TokenTypes.Bearer,
                    ExpiresIn = client.AccessTokenLifetime,
                };

                string algorithm = IdentityConstants.Algorithms.Asymmetric.RS256;
                var claims = authCodeGrant.Claims.ToClaimList();
                logger.SetUserScopeProperty(claims);
                var scopes = authCodeGrant.Scope.ToSpaceList();

                tokenResponse.AccessToken = await oidcJwtDownLogic.CreateAccessTokenAsync(client, claims, scopes, algorithm);
                var responseTypes = new[] { IdentityConstants.ResponseTypes.IdToken, IdentityConstants.ResponseTypes.Token };
                tokenResponse.IdToken = await oidcJwtDownLogic.CreateIdTokenAsync(client, claims, scopes, authCodeGrant.Nonce, responseTypes, null, tokenResponse.AccessToken, algorithm);

                if (scopes.Contains(IdentityConstants.DefaultOidcScopes.OfflineAccess))
                {
                    tokenResponse.RefreshToken = await oauthRefreshTokenGrantDownLogic.CreateRefreshTokenGrantAsync(client, claims, authCodeGrant.Scope);
                }

                logger.ScopeTrace(() => $"Token response '{tokenResponse.ToJsonIndented()}'.", traceType: TraceTypes.Message);
                logger.ScopeTrace(() => "AppReg, OIDC Token response.", triggerEvent: true);
                return new JsonResult(tokenResponse);
            }
            catch (KeyException kex)
            {
                throw new OAuthRequestException(kex.Message, kex) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.ServerError };
            }
        }

        protected override async Task<IActionResult> RefreshTokenGrantAsync(TClient client, TokenRequest tokenRequest)
        {
            (var refreshTokenGrant, var newRefreshToken) = await oauthRefreshTokenGrantDownLogic.ValidateAndUpdateRefreshTokenGrantAsync(client, tokenRequest.RefreshToken);
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
                    ExpiresIn = client.AccessTokenLifetime,
                };

                string algorithm = IdentityConstants.Algorithms.Asymmetric.RS256;
                var claims = refreshTokenGrant.Claims.ToClaimList();
                logger.SetUserScopeProperty(claims);

                tokenResponse.AccessToken = await oidcJwtDownLogic.CreateAccessTokenAsync(client, claims, scopes, algorithm);
                var responseTypes = new[] { IdentityConstants.ResponseTypes.IdToken, IdentityConstants.ResponseTypes.Token };
                tokenResponse.IdToken = await oidcJwtDownLogic.CreateIdTokenAsync(client, claims, scopes, null, responseTypes, null, tokenResponse.AccessToken, algorithm);

                if (!newRefreshToken.IsNullOrEmpty())
                {
                    tokenResponse.RefreshToken = newRefreshToken;
                }

                logger.ScopeTrace(() => $"Token response '{tokenResponse.ToJsonIndented()}'.", traceType: TraceTypes.Message);
                logger.ScopeTrace(() => "AppReg, OIDC Token response.", triggerEvent: true);
                return new JsonResult(tokenResponse);
            }
            catch (KeyException kex)
            {
                throw new OAuthRequestException(kex.Message, kex) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.ServerError };
            }
        }
    }
}
