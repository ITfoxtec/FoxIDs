using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OAuthTokenDownLogic<TParty, TClient, TScope, TClaim> : LogicBase where TParty : OAuthDownParty<TClient, TScope, TClaim> where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantRepository;
        private readonly JwtLogic<TClient, TScope, TClaim> jwtLogic;
        private readonly SecretHashLogic secretHashLogic;
        private readonly OAuthResourceScopeLogic<TClient, TScope, TClaim> oauthResourceScopeLogic;

        public OAuthTokenDownLogic(TelemetryScopedLogger logger, ITenantRepository tenantRepository, JwtLogic<TClient, TScope, TClaim> jwtLogic, SecretHashLogic secretHashLogic, OAuthResourceScopeLogic<TClient, TScope, TClaim> oauthResourceScopeLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.jwtLogic = jwtLogic;
            this.secretHashLogic = secretHashLogic;
            this.oauthResourceScopeLogic = oauthResourceScopeLogic;
        }

        public virtual async Task<IActionResult> TokenRequestAsync(string partyId)
        {
            logger.ScopeTrace("Down, OAuth Token request.");
            logger.SetScopeProperty("downPartyId", partyId);
            var party = await tenantRepository.GetAsync<TParty>(partyId);
            if (party.Client == null)
            {
                throw new NotSupportedException($"Party Client not configured.");
            }

            var formDictionary = HttpContext.Request.Form.ToDictionary();
            var tokenRequest = formDictionary.ToObject<TokenRequest>();
            var clientCredentials = formDictionary.ToObject<ClientCredentials>();

            logger.ScopeTrace($"Token request '{tokenRequest.ToJsonIndented()}'.");
            logger.SetScopeProperty("clientId", tokenRequest.ClientId);

            try
            {
                logger.SetScopeProperty("GrantType", tokenRequest.GrantType);
                switch (tokenRequest.GrantType)
                {
                    case IdentityConstants.GrantTypes.AuthorizationCode:
                        throw new NotImplementedException();
                    case IdentityConstants.GrantTypes.RefreshToken:
                        throw new NotImplementedException();
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

        protected void ValidateAuthCodeRequest(TClient client, TokenRequest tokenRequest)
        {
            tokenRequest.Validate();
            if (tokenRequest.RedirectUri.IsNullOrEmpty()) throw new ArgumentNullException(nameof(tokenRequest.RedirectUri), tokenRequest.GetTypeName());

            if (!client.RedirectUris.Any(u => u.Equals(tokenRequest.RedirectUri, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new OAuthRequestException($"Invalid redirect Uri '{tokenRequest.RedirectUri}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
            }

            if (!client.ClientId.Equals(tokenRequest.ClientId, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new OAuthRequestException($"Invalid client id '{tokenRequest.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidClient };
            }
        }
        protected void ValidateRefreshTokenRequest(TClient client, TokenRequest tokenRequest)
        {
            tokenRequest.Validate();

            if (!client.ClientId.Equals(tokenRequest.ClientId, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new OAuthRequestException($"Invalid client id '{tokenRequest.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidClient };
            }
        }
        protected void ValidateClientCredentialsRequest(TClient client, TokenRequest tokenRequest)
        {
            tokenRequest.Validate();

            if (!client.ClientId.Equals(tokenRequest.ClientId, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new OAuthRequestException($"Invalid client id '{tokenRequest.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidClient };
            }

            if (!tokenRequest.Scope.IsNullOrEmpty())
            {
                var resourceScopes = oauthResourceScopeLogic.GetResourceScopes(client as TClient);
                var invalidScope = tokenRequest.Scope.ToSpaceList().Where(s => !(resourceScopes.Select(rs => rs).Contains(s) || (client.Scopes != null && client.Scopes.Select(ps => ps.Scope).Contains(s))));
                if (invalidScope.Count() > 0)
                {
                    throw new OAuthRequestException($"Invalid scope '{tokenRequest.Scope}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidScope };
                }
            }
        }

        protected async Task ValidateSecret(TClient client, TokenRequest tokenRequest, ClientCredentials clientCredentials)
        {
            if (tokenRequest.ClientId.IsNullOrEmpty()) throw new ArgumentNullException(nameof(tokenRequest.ClientId), tokenRequest.GetTypeName());
            clientCredentials.Validate();

            if (client?.Secrets.Count() <= 0)
            {
                throw new OAuthRequestException($"Invalid client secret. Secret not configured for client id '{tokenRequest.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
            }

            foreach (var secret in client.Secrets)
            {
                if (await secretHashLogic.ValidateSecretAsync(secret, clientCredentials.ClientSecret))
                {
                    logger.ScopeTrace($"Down, OAuth Client id '{tokenRequest.ClientId}. Client secret valid.", triggerEvent: true);
                    return;
                }
            }

            throw new OAuthRequestException($"Invalid client secret for client id '{tokenRequest.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
        }

        protected virtual async Task<IActionResult> AuthorizationCodeGrant(TClient client, TokenRequest tokenRequest)
        {
            throw new NotImplementedException();
        }

        protected virtual Task<IActionResult> RefreshTokenGrant(TClient client, TokenRequest tokenRequest)
        {
            throw new NotImplementedException();
        }

        protected virtual async Task<IActionResult> ClientCredentialsGrant(TClient client, TokenRequest tokenRequest)
        {
            logger.ScopeTrace("Down, OAuth Client Credentials grant accepted.", triggerEvent: true);

            var tokenResponse = new TokenResponse
            {
                TokenType = IdentityConstants.TokenTypes.Bearer,
                ExpiresIn = client.AccessTokenLifetime,
            };

            string algorithm = IdentityConstants.Algorithms.Asymmetric.RS256;

            var claims = new List<Claim>();
            claims.AddClaim(JwtClaimTypes.Subject, $"c_{client.ClientId}");
            claims.AddClaim(JwtClaimTypes.AuthTime, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            //TODO should the amr claim be included???
            //claims.AddClaim(JwtClaimTypes.Amr, IdentityConstants.AuthenticationMethodReferenceValues.Pwd);

            var scopes = tokenRequest.Scope.ToSpaceList();

            tokenResponse.AccessToken = await jwtLogic.CreateAccessTokenAsync(client, claims, scopes, algorithm);

            logger.ScopeTrace($"Token response '{tokenResponse.ToJsonIndented()}'.");
            logger.ScopeTrace("Down, OAuth Token response.", triggerEvent: true);
            return new JsonResult(tokenResponse);
        }
    }
}
