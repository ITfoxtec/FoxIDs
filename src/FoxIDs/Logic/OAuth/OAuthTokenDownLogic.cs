using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
using ITfoxtec.Identity.Models;
using ITfoxtec.Identity.Tokens;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FoxIDs.Logic
{
    public class OAuthTokenDownLogic<TParty, TClient, TScope, TClaim> : LogicSequenceBase where TParty : OAuthDownParty<TClient, TScope, TClaim> where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly PlanUsageLogic planUsageLogic;
        private readonly OAuthJwtDownLogic<TClient, TScope, TClaim> oauthJwtDownLogic;
        private readonly SecretHashLogic secretHashLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic;
        private readonly OAuthTokenExchangeDownLogic<TParty, TClient, TScope, TClaim> oauthTokenExchangeDownLogic;

        public OAuthTokenDownLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, PlanUsageLogic planUsageLogic, OAuthJwtDownLogic<TClient, TScope, TClaim> oauthJwtDownLogic, SecretHashLogic secretHashLogic, ClaimTransformLogic claimTransformLogic, OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic, OAuthTokenExchangeDownLogic<TParty, TClient, TScope, TClaim> oauthTokenExchangeDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.planUsageLogic = planUsageLogic;
            this.oauthJwtDownLogic = oauthJwtDownLogic;
            this.secretHashLogic = secretHashLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.oauthResourceScopeDownLogic = oauthResourceScopeDownLogic;
            this.oauthTokenExchangeDownLogic = oauthTokenExchangeDownLogic;
        }

        public virtual async Task<IActionResult> TokenRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "AppReg, OAuth Token request.");
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
                        await ValidateClientAuthenticationAsync(party.Client, tokenRequest, HttpContext.Request.Headers, formDictionary);
                        return await ClientCredentialsGrantAsync(party, tokenRequest);
                    case IdentityConstants.GrantTypes.TokenExchange:
                        var tokenExchangeRequest = formDictionary.ToObject<TokenExchangeRequest>();
                        logger.ScopeTrace(() => $"AppReg, Token exchange request '{tokenExchangeRequest.ToJsonIndented()}'.", traceType: TraceTypes.Message);
                        oauthTokenExchangeDownLogic.ValidateTokenExchangeRequest(party.Client, tokenExchangeRequest);
                        await ValidateClientAuthenticationAsync(party.Client, tokenRequest, HttpContext.Request.Headers, formDictionary);
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

        protected void ValidateAuthCodeRequest(TClient client, TokenRequest tokenRequest)
        {
            tokenRequest.Validate();
            if (tokenRequest.RedirectUri.IsNullOrEmpty()) throw new ArgumentNullException(nameof(tokenRequest.RedirectUri), tokenRequest.GetTypeName());

            if (!client.RedirectUris.Any(u => client.DisableAbsoluteUris ? tokenRequest.RedirectUri?.StartsWith(u, StringComparison.InvariantCultureIgnoreCase) == true : u.Equals(tokenRequest.RedirectUri, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new OAuthRequestException($"Invalid redirect URI '{tokenRequest.RedirectUri}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
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
            if (client.DisableClientCredentialsGrant)
            {
                throw new OAuthRequestException($"Client credentials grant is disabled for client id '{tokenRequest.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.AccessDenied };
            }

            tokenRequest.Validate();

            if (client.ClientAuthenticationMethod == ClientAuthenticationMethods.ClientSecretPost || (client.ClientAuthenticationMethod == ClientAuthenticationMethods.PrivateKeyJwt && !tokenRequest.ClientId.IsNullOrWhiteSpace()))
            {
                if (client.ClientAuthenticationMethod == ClientAuthenticationMethods.ClientSecretPost)
                {
                    if (tokenRequest.ClientId.IsNullOrEmpty()) throw new ArgumentNullException(nameof(tokenRequest.ClientId), tokenRequest.GetTypeName());
                }
                if (!client.ClientId.Equals(tokenRequest.ClientId, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new OAuthRequestException($"Invalid client id '{tokenRequest.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidClient };
                }
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

        protected async Task ValidateClientAuthenticationAsync(TClient client, TokenRequest tokenRequest, IHeaderDictionary headers, Dictionary<string, string> formDictionary, bool clientAuthenticationRequired = true)
        {
            if (client.ClientAuthenticationMethod == ClientAuthenticationMethods.ClientSecretBasic) 
            {
                await ValidateClientSecretBasicAsync(client, headers, clientAuthenticationRequired);
            }
            else if (client.ClientAuthenticationMethod == ClientAuthenticationMethods.ClientSecretPost)
            {
                await ValidateClientSecretPostAsync(client, tokenRequest, formDictionary, clientAuthenticationRequired);
            }
            else if(client.ClientAuthenticationMethod == ClientAuthenticationMethods.PrivateKeyJwt)
            {
                await ValidateClientAssertionAsync(client, tokenRequest, formDictionary, clientAuthenticationRequired);
            }
            else
            {
                throw new NotImplementedException($"Client authentication method '{client.ClientAuthenticationMethod}' not implemented");
            }
        }

        private async Task ValidateClientSecretBasicAsync(TClient client, IHeaderDictionary headers, bool clientAuthenticationRequired = true)
        {
            if (!clientAuthenticationRequired && !(client.Secrets?.Count() > 0))
            {
                return;
            }

            var clientId = string.Empty;
            var clientSecret = string.Empty;
            var bearerHeader = headers.GetAuthorizationHeaderBearer();
            if (!bearerHeader.IsNullOrEmpty())
            {
                var bearerHeaderSplit = bearerHeader.Base64Decode()?.Split(':');
                if (bearerHeaderSplit?.Count() == 2)
                {
                    clientId = WebUtility.UrlDecode(bearerHeaderSplit[0]);
                    clientSecret = WebUtility.UrlDecode(bearerHeaderSplit[1]);
                }
            }

            logger.ScopeTrace(() => $"AppReg, Client credentials basic '{new { ClientId = clientId, ClientSecret = $"{(clientSecret?.Length > 10 ? clientSecret.Substring(0, 3) : string.Empty)}..." }.ToJsonIndented()}'.", traceType: TraceTypes.Message);
            try
            {
                if (clientId.IsNullOrEmpty() || clientSecret.IsNullOrEmpty()) throw new ArgumentException("Client id or secret is null or empty.");
                if (clientId.Length > IdentityConstants.MessageLength.ClientIdMax)
                {
                    throw new ArgumentException($"Invalid client id, max length {IdentityConstants.MessageLength.ClientIdMax}.");
                }
                if (clientSecret.Length > IdentityConstants.MessageLength.ClientSecretMax)
                {
                    throw new ArgumentException($"Invalid client secret, max length {IdentityConstants.MessageLength.ClientSecretMax}.");
                }

                if (!client.ClientId.Equals(clientId, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new OAuthRequestException($"Invalid client id '{clientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidClient };
                }

                await ValidateClientSecretAsync(client, clientSecret, clientAuthenticationRequired);
            }
            catch (Exception ex)
            {
                throw new OAuthRequestException($"Client authentication, client secret basic. {ex.Message}{(ex is ArgumentNullException ? " is null or empty." : string.Empty)}", ex) { RouteBinding = RouteBinding, Error = ex is OAuthRequestException oare ? oare.Error : IdentityConstants.ResponseErrors.AccessDenied };
            }
        }

        private async Task ValidateClientSecretPostAsync(TClient client, TokenRequest tokenRequest, Dictionary<string, string> formDictionary, bool clientAuthenticationRequired = true)
        {
            if (!clientAuthenticationRequired && !(client.Secrets?.Count() > 0))
            {
                return;
            }

            var clientCredentials = formDictionary.ToObject<ClientCredentials>();
            logger.ScopeTrace(() => $"AppReg, Client credentials post '{new ClientCredentials { ClientSecret = $"{(clientCredentials.ClientSecret?.Length > 10 ? clientCredentials.ClientSecret.Substring(0, 3) : string.Empty)}..." }.ToJsonIndented()}'.", traceType: TraceTypes.Message);
            try
            {
                clientCredentials.Validate();

                await ValidateClientSecretAsync(client, clientCredentials.ClientSecret, clientAuthenticationRequired);
            }
            catch (Exception ex)
            {
                throw new OAuthRequestException($"Client authentication, client secret post. {ex.Message}{(ex is ArgumentNullException ? " is null or empty." : string.Empty)}", ex) { RouteBinding = RouteBinding, Error = ex is OAuthRequestException oare ? oare.Error : IdentityConstants.ResponseErrors.AccessDenied };
            }
        }

        private async Task ValidateClientAssertionAsync(TClient client, TokenRequest tokenRequest, Dictionary<string, string> formDictionary, bool clientAuthenticationRequired = true)
        {
            if (!clientAuthenticationRequired && !(client.ClientKeys?.Count() > 0))
            {
                return;
            }

            var clientAssertionCredentials = formDictionary.ToObject<ClientAssertionCredentials>();
            logger.ScopeTrace(() => $"AppReg, Client credentials assertion '{clientAssertionCredentials.ToJsonIndented()}'.", traceType: TraceTypes.Message);
            try
            {
                clientAssertionCredentials.Validate();

                if (clientAssertionCredentials.ClientAssertionType != IdentityConstants.ClientAssertionTypes.JwtBearer)
                {
                    throw new OAuthRequestException($"Invalid client assertion type, supported types ['{IdentityConstants.ClientAssertionTypes.JwtBearer}']. Client id '{client.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidClient };
                }

                if (client?.ClientKeys.Count() <= 0)
                {
                    throw new OAuthRequestException($"Invalid client key (certificate). Key (certificate) not configured for client id '{client.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidClient };
                }
                else
                {
                    var validClientKeys = new List<JsonWebKey>();
                    var clientKeyAndCertificates = client.ClientKeys.Select(c => new { Key = c, Certificate = c.ToX509Certificate() }).ToList();
                    foreach(var clientKeyAndCertificate in clientKeyAndCertificates)
                    {
                        if (clientKeyAndCertificate.Certificate.IsValidateCertificate())
                        {
                            validClientKeys.Add(clientKeyAndCertificate.Key);
                        }
                    }
                    if(validClientKeys.Count <= 0)
                    {
                        clientKeyAndCertificates.First().Certificate.ValidateCertificate($"Client (client id '{client.ClientId}') key");
                    }

                    var clientAssertion = JwtHandler.ReadToken(clientAssertionCredentials.ClientAssertion);
                    if (!client.ClientId.Equals(clientAssertion.Issuer))
                    {
                        throw new OAuthRequestException($"Client credentials assertion issuer '{clientAssertion.Issuer}' is invalid. It should be client id '{client.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidClient };
                    }

                    var tokenEndpoint = UrlCombine.Combine(HttpContext.GetHostWithTenantAndTrack(), RouteBinding.PartyNameAndBinding, Constants.Routes.OAuthController, Constants.Endpoints.Token);
                    var claimsPrincipal = await oauthJwtDownLogic.ValidateClientAssertionAsync(clientAssertionCredentials.ClientAssertion, client.ClientId, validClientKeys, tokenEndpoint);

                    if (!client.ClientId.Equals(claimsPrincipal.Claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Subject)))
                    {
                        throw new OAuthRequestException($"Client credentials assertion {JwtClaimTypes.Subject} is invalid for client id '{client.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidClient };
                    }

                    var jwtId = claimsPrincipal.Claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.JwtId);
                    if (!jwtId.IsNullOrWhiteSpace())
                    {
                        // TODO
                        // The "jti" (JWT ID) claim provides a unique identifier for the token. Ensure that JWTs are not replayed by maintaining the set of used "jti" values for the length of time for which the JWT would be considered valid based on the applicable "exp" instant.
                    }
                }
            }
            catch (Exception ex)
            {
                throw new OAuthRequestException($"Client authentication, client credentials assertion. {ex.Message}{(ex is ArgumentNullException ? " is null or empty." : string.Empty)}", ex) { RouteBinding = RouteBinding, Error = ex is OAuthRequestException oare ? oare.Error : IdentityConstants.ResponseErrors.AccessDenied };
            }
        }

        private async Task ValidateClientSecretAsync(TClient client, string clientSecret, bool clientAuthenticationRequired = true)
        {
            if (client?.Secrets?.Count() > 0)
            {
                foreach (var secret in client.Secrets)
                {
                    if (await secretHashLogic.ValidateSecretAsync(secret, clientSecret))
                    {
                        logger.ScopeTrace(() => $"AppReg, OAuth client id '{client.ClientId}. Client secret valid.", triggerEvent: true);
                        return;
                    }
                }

                throw new OAuthRequestException($"Invalid client secret for client id '{client.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.AccessDenied };
            }
            else
            {
                if (clientAuthenticationRequired)
                {
                    throw new OAuthRequestException($"Invalid client secret. Secret not configured for client id '{client.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidClient };
                }
            }
        }

        protected async Task ValidatePkceAsync(TClient client, string codeChallenge, string codeChallengeMethod, CodeVerifierSecret codeVerifierSecret)
        {
            codeVerifierSecret.Validate();

            if(codeChallengeMethod.IsNullOrEmpty() || codeChallengeMethod.Equals(IdentityConstants.CodeChallengeMethods.Plain, StringComparison.Ordinal)) 
            {
                if(!codeVerifierSecret.CodeVerifier.Equals(codeChallenge, StringComparison.Ordinal))
                {
                    throw new OAuthRequestException($"Invalid '{IdentityConstants.CodeChallengeMethods.Plain}' code verifier (PKCE) for client id '{client.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
                }
            }
            else if (codeChallengeMethod.Equals(IdentityConstants.CodeChallengeMethods.S256, StringComparison.Ordinal))
            {
                var codeChallengeFromCodeVerifier = await codeVerifierSecret.CodeVerifier.Sha256HashBase64urlEncodedAsync();
                if (!codeChallengeFromCodeVerifier.Equals(codeChallenge, StringComparison.Ordinal))
                {
                    throw new OAuthRequestException($"Invalid '{IdentityConstants.CodeChallengeMethods.S256}' code verifier (PKCE) for client id '{client.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
                }
            }
            else
            {
                throw new OAuthRequestException($"Invalid code challenge method (PKCE) for client id '{client.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
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
            logger.ScopeTrace(() => "AppReg, OAuth Client Credentials grant accepted.", triggerEvent: true);

            try
            {
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

                logger.ScopeTrace(() => $"AppReg, OAuth created JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
                claims = await claimTransformLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
                logger.ScopeTrace(() => $"AppReg, OAuth output JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
                logger.SetUserScopeProperty(claims);

                var scopes = tokenRequest.Scope.ToSpaceList();
                tokenResponse.AccessToken = await oauthJwtDownLogic.CreateAccessTokenAsync(party.Client, claims, scopes, algorithm);

                planUsageLogic.LogTokenRequestEvent(UsageLogTokenTypes.ClientCredentials);

                logger.ScopeTrace(() => $"Token response '{tokenResponse.ToJsonIndented()}'.", traceType: TraceTypes.Message);
                logger.ScopeTrace(() => "AppReg, OAuth Token response.", triggerEvent: true);
                return new JsonResult(tokenResponse);
            }
            catch (KeyException kex)
            {
                throw new OAuthRequestException(kex.Message, kex) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.ServerError };
            }
        }
    }
}
