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
        private readonly JwtDownLogic<TClient, TScope, TClaim> jwtDownLogic;
        private readonly SecretHashLogic secretHashLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic;

        public OAuthTokenDownLogic(TelemetryScopedLogger logger, ITenantRepository tenantRepository, JwtDownLogic<TClient, TScope, TClaim> jwtDownLogic, SecretHashLogic secretHashLogic, ClaimTransformLogic claimTransformLogic, OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.jwtDownLogic = jwtDownLogic;
            this.secretHashLogic = secretHashLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.oauthResourceScopeDownLogic = oauthResourceScopeDownLogic;
        }

        public virtual async Task<IActionResult> TokenRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "Down, OAuth Token request.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantRepository.GetAsync<TParty>(partyId);
            if (party.Client == null)
            {
                throw new NotSupportedException("Party Client not configured.");
            }
            logger.SetScopeProperty(Constants.Logs.DownPartyClientId, party.Client.ClientId);

            var formDictionary = HttpContext.Request.Form.ToDictionary();
            var tokenRequest = formDictionary.ToObject<TokenRequest>();
            logger.ScopeTrace(() => $"Down, Token request '{tokenRequest.ToJsonIndented()}'.", traceType: TraceTypes.Message);

            var clientCredentials = formDictionary.ToObject<ClientCredentials>();
            logger.ScopeTrace(() => $"Down, Client credentials '{new ClientCredentials { ClientSecret = $"{(clientCredentials.ClientSecret?.Length > 10 ? clientCredentials.ClientSecret.Substring(0, 3) : string.Empty)}..." }.ToJsonIndented()}'.", traceType: TraceTypes.Message);

            try
            {
                logger.SetScopeProperty(Constants.Logs.GrantType, tokenRequest.GrantType);
                switch (tokenRequest.GrantType)
                {
                    case IdentityConstants.GrantTypes.AuthorizationCode:
                        throw new NotImplementedException();
                    case IdentityConstants.GrantTypes.RefreshToken:
                        throw new NotImplementedException();
                    case IdentityConstants.GrantTypes.ClientCredentials:
                        ValidateClientCredentialsRequest(party.Client, tokenRequest);
                        await ValidateSecretAsync(party.Client, tokenRequest, clientCredentials);
                        return await ClientCredentialsGrantAsync(party, tokenRequest);
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
                var resourceScopes = oauthResourceScopeDownLogic.GetResourceScopes(client as TClient);
                var invalidScope = tokenRequest.Scope.ToSpaceList().Where(s => !(resourceScopes.Select(rs => rs).Contains(s) || (client.Scopes != null && client.Scopes.Select(ps => ps.Scope).Contains(s))));
                if (invalidScope.Count() > 0)
                {
                    throw new OAuthRequestException($"Invalid scope '{tokenRequest.Scope}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidScope };
                }
            }
        }

        protected async Task ValidateSecretAsync(TClient client, TokenRequest tokenRequest, ClientCredentials clientCredentials, bool secretValidationRequired = true)
        {
            if(!secretValidationRequired && clientCredentials.ClientSecret.IsNullOrEmpty())
            {
                return;
            }

            if (tokenRequest.ClientId.IsNullOrEmpty()) throw new ArgumentNullException(nameof(tokenRequest.ClientId), tokenRequest.GetTypeName());
            clientCredentials.Validate();

            if(client?.Secrets.Count() <= 0)
            {
                if(secretValidationRequired)
                {
                    throw new OAuthRequestException($"Invalid client secret. Secret not configured for client id '{tokenRequest.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
                }
            }
            else
            {
                foreach (var secret in client.Secrets)
                {
                    if (await secretHashLogic.ValidateSecretAsync(secret, clientCredentials.ClientSecret))
                    {
                        logger.ScopeTrace(() => $"Down, OAuth Client id '{tokenRequest.ClientId}. Client secret valid.", triggerEvent: true);
                        return;
                    }
                }

                throw new OAuthRequestException($"Invalid client secret for client id '{tokenRequest.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
            }
        }

        protected async Task ValidatePkceAsync(TClient client, string codeChallenge, string codeChallengeMethod, CodeVerifierSecret codeVerifierSecret)
        {
            codeVerifierSecret.Validate();

            if(codeChallengeMethod.IsNullOrEmpty() || codeChallengeMethod.Equals(IdentityConstants.CodeChallengeMethods.Plain, StringComparison.Ordinal)) 
            {
                if(!codeVerifierSecret.CodeVerifier.Equals(codeChallenge, StringComparison.Ordinal))
                {
                    throw new OAuthRequestException($"Invalid '{IdentityConstants.CodeChallengeMethods.Plain}' code verifier for client id '{client.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
                }
            }
            else if (codeChallengeMethod.Equals(IdentityConstants.CodeChallengeMethods.S256, StringComparison.Ordinal))
            {
                var codeChallengeFromCodeVerifier = await codeVerifierSecret.CodeVerifier.Sha256HashBase64urlEncodedAsync();
                if (!codeChallengeFromCodeVerifier.Equals(codeChallenge, StringComparison.Ordinal))
                {
                    throw new OAuthRequestException($"Invalid '{IdentityConstants.CodeChallengeMethods.S256}' code verifier for client id '{client.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
                }
            }
            else
            {
                throw new OAuthRequestException($"Invalid code callenge method for client id '{client.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
            }
        }

        protected virtual Task<IActionResult> AuthorizationCodeGrantAsync(TClient client, TokenRequest tokenRequest, bool validatePkce, CodeVerifierSecret codeVerifierSecret)
        {
            throw new NotImplementedException();
        }

        protected virtual Task<IActionResult> RefreshTokenGrantAsync(TClient client, TokenRequest tokenRequest)
        {
            throw new NotImplementedException();
        }

        protected virtual async Task<IActionResult> ClientCredentialsGrantAsync(TParty party, TokenRequest tokenRequest)
        {
            logger.ScopeTrace(() => "Down, OAuth Client Credentials grant accepted.", triggerEvent: true);
            if (party.Client == null)
            {
                throw new NotSupportedException("Party Client not configured.");
            }

            var tokenResponse = new TokenResponse
            {
                TokenType = IdentityConstants.TokenTypes.Bearer,
                ExpiresIn = party.Client.AccessTokenLifetime,
            };

            string algorithm = IdentityConstants.Algorithms.Asymmetric.RS256;

            var claims = new List<Claim>();
            claims.AddClaim(JwtClaimTypes.Subject, $"c_{party.Client.ClientId}");
            claims.AddClaim(JwtClaimTypes.AuthTime, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            //TODO should the amr claim be included???
            //claims.AddClaim(JwtClaimTypes.Amr, IdentityConstants.AuthenticationMethodReferenceValues.Pwd);

            logger.ScopeTrace(() => $"Down, OAuth created JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            claims = await claimTransformLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
            logger.ScopeTrace(() => $"Down, OAuth output JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);

            var scopes = tokenRequest.Scope.ToSpaceList();

            tokenResponse.AccessToken = await jwtDownLogic.CreateAccessTokenAsync(party.Client, claims, scopes, algorithm);

            logger.ScopeTrace(() => $"Token response '{tokenResponse.ToJsonIndented()}'.", traceType: TraceTypes.Message);
            logger.ScopeTrace(() => "Down, OAuth Token response.", triggerEvent: true);
            return new JsonResult(tokenResponse);
        }
    }
}
