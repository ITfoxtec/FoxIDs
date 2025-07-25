﻿using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
using ITfoxtec.Identity.Models;
using ITfoxtec.Identity.Tokens;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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

            var formDictionary = GetFormDictionary();
            var tokenRequest = formDictionary.ToObject<TokenRequest>();
            if (tokenRequest.GrantType != IdentityConstants.GrantTypes.TokenExchange)
            {
                logger.ScopeTrace(() => $"AppReg, Token request '{tokenRequest.ToJson()}'.", traceType: TraceTypes.Message);
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

        protected void ValidateAuthCodeRequest(TClient client, TokenRequest tokenRequest)
        {
            tokenRequest.Validate();
            if (tokenRequest.RedirectUri.IsNullOrEmpty()) throw new ArgumentNullException(nameof(tokenRequest.RedirectUri), tokenRequest.GetTypeName());

            if (!client.RedirectUris.Any(u => client.DisableAbsoluteUris ? tokenRequest.RedirectUri?.StartsWith(u, StringComparison.InvariantCultureIgnoreCase) == true : u.Equals(tokenRequest.RedirectUri, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new OAuthRequestException($"Invalid redirect URI '{tokenRequest.RedirectUri}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidGrant };
            }
        }
        protected void ValidateRefreshTokenRequest(TClient client, TokenRequest tokenRequest)
        {
            tokenRequest.Validate();
        }
        protected void ValidateClientCredentialsRequest(TClient client, TokenRequest tokenRequest)
        {
            if (client.DisableClientCredentialsGrant)
            {
                throw new OAuthRequestException($"Client credentials grant is disabled for client id '{client.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.AccessDenied };
            }

            tokenRequest.Validate();

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

        private void ValidateClientId(TClient client, TokenRequest tokenRequest)
        {
            if (tokenRequest.ClientId.IsNullOrEmpty()) throw new ArgumentNullException(nameof(tokenRequest.ClientId), tokenRequest.GetTypeName());

            if (!client.ClientId.Equals(tokenRequest.ClientId, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new OAuthRequestException($"Invalid client id '{tokenRequest.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidClient };
            }
        }

        protected async Task ValidateClientAuthenticationAsync(TParty party, TokenRequest tokenRequest, IHeaderDictionary headers, Dictionary<string, string> formDictionary, bool clientAuthenticationRequired = true)
        {
            if (party.Client.ClientAuthenticationMethod == ClientAuthenticationMethods.ClientSecretBasic) 
            {
                await ValidateClientSecretBasicAsync(party.Client, tokenRequest, headers, clientAuthenticationRequired);
            }
            else if (party.Client.ClientAuthenticationMethod == ClientAuthenticationMethods.ClientSecretPost)
            {
                await ValidateClientSecretPostAsync(party.Client, tokenRequest, headers, formDictionary, clientAuthenticationRequired);
            }
            else if(party.Client.ClientAuthenticationMethod == ClientAuthenticationMethods.PrivateKeyJwt)
            {
                await ValidateClientAssertionAsync(party.Client, party.UsePartyIssuer, tokenRequest, formDictionary, clientAuthenticationRequired);
            }
            else
            {
                throw new NotImplementedException($"Client authentication method '{party.Client.ClientAuthenticationMethod}' not implemented");
            }
        }

        private async Task ValidateClientSecretBasicAsync(TClient client, TokenRequest tokenRequest, IHeaderDictionary headers, bool clientAuthenticationRequired = true)
        {
            if (!clientAuthenticationRequired && !(client.Secrets?.Count() > 0))
            {
                ValidateClientId(client, tokenRequest);
                return;
            }

            (var clientId, var clientSecret) = headers.GetAuthorizationHeaderBasic();
            logger.ScopeTrace(() => $"AppReg, Client credentials basic '{new { ClientId = clientId, ClientSecret = clientSecret?.Length > 10 ? $"{clientSecret.Substring(0, 3)}..." : "hidden" }.ToJson()}'.", traceType: TraceTypes.Message);
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

        private async Task ValidateClientSecretPostAsync(TClient client, TokenRequest tokenRequest, IHeaderDictionary headers, Dictionary<string, string> formDictionary, bool clientAuthenticationRequired = true)
        {
            if (!clientAuthenticationRequired && !(client.Secrets?.Count() > 0))
            {
                ValidateClientId(client, tokenRequest);
                return;
            }

            var clientCredentials = formDictionary.ToObject<ClientCredentials>();
            logger.ScopeTrace(() => $"AppReg, Client credentials post '{new ClientCredentials { ClientSecret = $"{(clientCredentials.ClientSecret?.Length > 10 ? clientCredentials.ClientSecret.Substring(0, 3) : string.Empty)}..." }.ToJson()}'.", traceType: TraceTypes.Message);

            if (clientCredentials.ClientSecret.IsNullOrWhiteSpace() && !headers.GetAuthorizationHeader(IdentityConstants.BasicAuthentication.Basic).IsNullOrEmpty())
            {
                logger.ScopeTrace(() => "AppReg, Default to use Client credentials basic.");
                await ValidateClientSecretBasicAsync(client, tokenRequest, headers, clientAuthenticationRequired);
            }
            else
            {
                try
                {
                    ValidateClientId(client, tokenRequest);
                    clientCredentials.Validate();

                    await ValidateClientSecretAsync(client, clientCredentials.ClientSecret, clientAuthenticationRequired);
                }
                catch (Exception ex)
                {
                    throw new OAuthRequestException($"Client authentication, client secret post. {ex.Message}{(ex is ArgumentNullException ? " is null or empty." : string.Empty)}", ex) { RouteBinding = RouteBinding, Error = ex is OAuthRequestException oare ? oare.Error : IdentityConstants.ResponseErrors.AccessDenied };
                }
            }
        }

        private async Task ValidateClientAssertionAsync(TClient client, bool usePartyIssuer, TokenRequest tokenRequest, Dictionary<string, string> formDictionary, bool clientAuthenticationRequired = true)
        {
            if (!clientAuthenticationRequired && !(client.ClientKeys?.Count() > 0))
            {
                ValidateClientId(client, tokenRequest);
                return;
            }

            var clientAssertionCredentials = formDictionary.ToObject<ClientAssertionCredentials>();
            logger.ScopeTrace(() => $"AppReg, Client credentials assertion '{clientAssertionCredentials.ToJson()}'.", traceType: TraceTypes.Message);
            try
            {
                if(!tokenRequest.ClientId.IsNullOrWhiteSpace())
                {
                    ValidateClientId(client, tokenRequest);
                }
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

                    var tokenEndpoint = UrlCombine.Combine(HttpContext.GetHostWithRouteOrBinding(usePartyIssuer), Constants.Routes.OAuthController, Constants.Endpoints.Token);
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

        protected virtual Task<IActionResult> AuthorizationCodeGrantAsync(TParty party, TokenRequest tokenRequest, bool validatePkce, CodeVerifierSecret codeVerifierSecret)
        {
            throw new NotImplementedException();
        }

        protected virtual Task<IActionResult> RefreshTokenGrantAsync(TParty party, TokenRequest tokenRequest)
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
                claims = await claimTransformLogic.TransformAsync(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
                logger.ScopeTrace(() => $"AppReg, OAuth output JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
                logger.SetUserScopeProperty(claims);

                var scopes = tokenRequest.Scope.ToSpaceList();
                tokenResponse.AccessToken = await oauthJwtDownLogic.CreateAccessTokenAsync(party.Client, party.UsePartyIssuer ? RouteBinding.RouteUrl : null, claims, scopes, algorithm);

                logger.ScopeTrace(() => $"Token response '{tokenResponse.ToJson()}'.", traceType: TraceTypes.Message);
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
