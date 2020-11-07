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
        private readonly ITenantRepository tenantRepository;
        private readonly JwtLogic<TClient, TScope, TClaim> jwtLogic;
        private readonly OAuthAuthCodeGrantLogic<TClient, TScope, TClaim> oauthAuthCodeGrantLogic;
        private readonly OAuthRefreshTokenGrantLogic<TClient, TScope, TClaim> oauthRefreshTokenGrantLogic;

        public OidcTokenDownLogic(TelemetryScopedLogger logger, ITenantRepository tenantRepository, JwtLogic<TClient, TScope, TClaim> jwtLogic, OAuthAuthCodeGrantLogic<TClient, TScope, TClaim> oauthAuthCodeGrantLogic, OAuthRefreshTokenGrantLogic<TClient, TScope, TClaim> oauthRefreshTokenGrantLogic, SecretHashLogic secretHashLogic, OAuthResourceScopeLogic<TClient, TScope, TClaim> oauthResourceScopeLogic, IHttpContextAccessor httpContextAccessor) : base(logger, tenantRepository, jwtLogic, secretHashLogic, oauthResourceScopeLogic, httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.jwtLogic = jwtLogic;
            this.oauthAuthCodeGrantLogic = oauthAuthCodeGrantLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
        }

        public override async Task<IActionResult> TokenRequestAsync(string partyId)
        {
            logger.ScopeTrace("Down, OIDC Token request.");
            logger.SetScopeProperty("downPartyId", partyId);
            var party = await tenantRepository.GetAsync<TParty>(partyId);
            if (party.Client == null)
            {
                throw new NotSupportedException($"Party Client not configured.");
            }

            var formDictionary = HttpContext.Request.Form.ToDictionary();
            var tokenRequest = formDictionary.ToObject<TokenRequest>();

            logger.ScopeTrace($"Token request '{tokenRequest.ToJsonIndented()}'.");
            logger.SetScopeProperty("clientId", tokenRequest.ClientId);

            var clientCredentials = formDictionary.ToObject<ClientCredentials>();

            var codeVerifierSecret = party.Client.RequirePkce ? formDictionary.ToObject<CodeVerifierSecret>() : null;

            try
            {
                logger.SetScopeProperty("GrantType", tokenRequest.GrantType);
                switch (tokenRequest.GrantType)
                {
                    case IdentityConstants.GrantTypes.AuthorizationCode:
                        ValidateAuthCodeRequest(party.Client, tokenRequest);
                        var validatePkce = party.Client.RequirePkce && codeVerifierSecret != null;
                        await ValidateSecret(party.Client, tokenRequest, clientCredentials, secretValidationRequered: !validatePkce);
                        return await AuthorizationCodeGrant(party.Client, tokenRequest, validatePkce, codeVerifierSecret);
                    case IdentityConstants.GrantTypes.RefreshToken:
                        ValidateRefreshTokenRequest(party.Client, tokenRequest);
                        await ValidateSecret(party.Client, tokenRequest, clientCredentials, secretValidationRequered: !party.Client.RequirePkce);
                        return await RefreshTokenGrant(party.Client, tokenRequest);
                    case IdentityConstants.GrantTypes.ClientCredentials:
                        ValidateClientCredentialsRequest(party.Client, tokenRequest);
                        await ValidateSecret(party.Client, tokenRequest, clientCredentials);
                        return await ClientCredentialsGrant(party.Client, tokenRequest);
                    case IdentityConstants.GrantTypes.Delegation:
                        throw new NotImplementedException();

                    default:
                        throw new OAuthRequestException($"Unsupported grant type '{tokenRequest.GrantType}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.UnsupportedGrantType };
                }
            }
            catch (ArgumentException ex)
            {
                throw new OAuthRequestException(ex.Message, ex) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
            }
        }

        protected override async Task<IActionResult> AuthorizationCodeGrant(TClient client, TokenRequest tokenRequest, bool validatePkce, CodeVerifierSecret codeVerifierSecret)
        {
            var authCodeGrant = await oauthAuthCodeGrantLogic.GetAndValidateAuthCodeGrantAsync(tokenRequest.Code, tokenRequest.RedirectUri, tokenRequest.ClientId);
            Console.WriteLine($"authCodeGrant not null: {authCodeGrant != null}");
            if (validatePkce)
            {
                await ValidatePkce(client, authCodeGrant.CodeChallenge, authCodeGrant.CodeChallengeMethod, codeVerifierSecret);
            }
            logger.ScopeTrace("Down, OIDC Authorization code grant accepted.", triggerEvent: true);

            var tokenResponse = new TokenResponse
            {
                TokenType = IdentityConstants.TokenTypes.Bearer,
                ExpiresIn = client.AccessTokenLifetime,
            };

            string algorithm = IdentityConstants.Algorithms.Asymmetric.RS256;
            var claims = authCodeGrant.Claims.ToClaimList();
            var scopes = authCodeGrant.Scope.ToSpaceList();
   
            tokenResponse.AccessToken = await jwtLogic.CreateAccessTokenAsync(client, claims, scopes, algorithm);
            var responseTypes = new[] { IdentityConstants.ResponseTypes.IdToken, IdentityConstants.ResponseTypes.Token };
            tokenResponse.IdToken = await jwtLogic.CreateIdTokenAsync(client, claims, scopes, authCodeGrant.Nonce, responseTypes, null, tokenResponse.AccessToken, algorithm);

            if (scopes.Contains(IdentityConstants.DefaultOidcScopes.OfflineAccess))
            {
                tokenResponse.RefreshToken = await oauthRefreshTokenGrantLogic.CreateRefreshTokenGrantAsync(client, claims, authCodeGrant.Scope);
            }

            logger.ScopeTrace($"Token response '{tokenResponse.ToJsonIndented()}'.");
            logger.ScopeTrace("Down, OIDC Token response.", triggerEvent: true);
            return new JsonResult(tokenResponse);
        }

        protected override async Task<IActionResult> RefreshTokenGrant(TClient client, TokenRequest tokenRequest)
        {
            (var refreshTokenGrant, var newRefreshToken) = await oauthRefreshTokenGrantLogic.ValidateAndUpdateRefreshTokenGrantAsync(client, tokenRequest.RefreshToken);
            logger.ScopeTrace("Down, OIDC Refresh Token grant accepted.", triggerEvent: true);

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

            var tokenResponse = new TokenResponse
            {
                TokenType = IdentityConstants.TokenTypes.Bearer,
                ExpiresIn = client.AccessTokenLifetime,
            };

            string algorithm = IdentityConstants.Algorithms.Asymmetric.RS256;
            var claims = refreshTokenGrant.Claims.ToClaimList();

            tokenResponse.AccessToken = await jwtLogic.CreateAccessTokenAsync(client, claims, scopes, algorithm);
            var responseTypes = new[] { IdentityConstants.ResponseTypes.IdToken, IdentityConstants.ResponseTypes.Token };
            tokenResponse.IdToken = await jwtLogic.CreateIdTokenAsync(client, claims, scopes, null, responseTypes, null, tokenResponse.AccessToken, algorithm);

            if (!newRefreshToken.IsNullOrEmpty())
            {
                tokenResponse.RefreshToken = newRefreshToken;
            }

            logger.ScopeTrace($"Token response '{tokenResponse.ToJsonIndented()}'.");
            logger.ScopeTrace("Down, OIDC Token response.", triggerEvent: true);
            return new JsonResult(tokenResponse);
        }
    }
}
