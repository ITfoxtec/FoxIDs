using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Tokens;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using UrlCombineLib;

namespace FoxIDs.Logic
{
    public class OidcAuthUpLogic<TParty, TClient> : LogicBase where TParty : OidcUpParty<TClient> where TClient : OidcUpClient
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly FormActionLogic formActionLogic;
        private readonly OidcDiscoveryReadUpLogic oidcDiscoveryReadUpLogic;
        private readonly ClaimTransformationsLogic claimTransformationsLogic;
        private readonly IHttpClientFactory httpClientFactory;

        public OidcAuthUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, SessionUpPartyLogic sessionUpPartyLogic, FormActionLogic formActionLogic, OidcDiscoveryReadUpLogic oidcDiscoveryReadUpLogic, ClaimTransformationsLogic claimTransformationsLogic, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.formActionLogic = formActionLogic;
            this.oidcDiscoveryReadUpLogic = oidcDiscoveryReadUpLogic;
            this.claimTransformationsLogic = claimTransformationsLogic;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> AuthenticationRequestRedirectAsync(UpPartyLink partyLink, LoginRequest loginRequest)
        {
            logger.ScopeTrace("Up, OIDC Authentication request redirect.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty("upPartyId", partyId);

            await loginRequest.ValidateObjectAsync();

            var oidcUpSequenceData = new OidcUpSequenceData
            {
                DownPartyId = loginRequest.DownParty.Id,
                DownPartyType = loginRequest.DownParty.Type,
                UpPartyId = partyId,
                LoginAction = loginRequest.LoginAction,
                UserId = loginRequest.UserId,
                MaxAge = loginRequest.MaxAge
            };
            await sequenceLogic.SaveSequenceDataAsync(oidcUpSequenceData);

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.OAuthUpJumpController, Constants.Endpoints.UpJump.AuthenticationRequest, includeSequence: true).ToRedirectResult();
        }

        public async Task<IActionResult> AuthenticationRequestAsync(string partyId)
        {
            logger.ScopeTrace("Up, OIDC Authentication request.");
            var oidcUpSequenceData = await sequenceLogic.GetSequenceDataAsync<OidcUpSequenceData>(remove: false);
            if (!oidcUpSequenceData.UpPartyId.Equals(partyId, StringComparison.Ordinal))
            {
                throw new Exception("Invalid up-party id.");
            }
            logger.SetScopeProperty("upPartyId", oidcUpSequenceData.UpPartyId);

            var party = await tenantRepository.GetAsync<OidcUpParty>(oidcUpSequenceData.UpPartyId);
            logger.SetScopeProperty("upPartyClientId", party.Client.ClientId);

            await oidcDiscoveryReadUpLogic.CheckOidcDiscoveryAndUpdatePartyAsync(party);

            var nonce = RandomGenerator.GenerateNonce();
            var loginCallBackUrl = HttpContext.GetUpPartyUrl(party.Name, Constants.Routes.OAuthController, Constants.Endpoints.AuthorizationResponse, partyBindingPattern: party.PartyBindingPattern);

            oidcUpSequenceData.ClientId = !party.Client.SpClientId.IsNullOrWhiteSpace() ? party.Client.SpClientId : party.Client.ClientId;
            oidcUpSequenceData.RedirectUri = loginCallBackUrl;
            oidcUpSequenceData.Nonce = nonce;  
            if (party.Client.EnablePkce)
            {
                var codeVerifier = RandomGenerator.Generate(64);
                oidcUpSequenceData.CodeVerifier = codeVerifier;
            }
            await sequenceLogic.SaveSequenceDataAsync(oidcUpSequenceData);

            var authenticationRequest = new AuthenticationRequest
            {
                ClientId = oidcUpSequenceData.ClientId,
                ResponseMode = party.Client.ResponseMode,
                ResponseType = party.Client.ResponseType,
                RedirectUri = loginCallBackUrl,
                Nonce = nonce,
                State = SequenceString
            };
            logger.ScopeTrace($"Authentication request '{authenticationRequest.ToJsonIndented()}'.");

            switch (oidcUpSequenceData.LoginAction)
            {
                case LoginAction.ReadSession:
                    authenticationRequest.Prompt = IdentityConstants.AuthorizationServerPrompt.None;
                    break;
                case LoginAction.RequireLogin:
                    authenticationRequest.Prompt = IdentityConstants.AuthorizationServerPrompt.Login;
                    break;
                default:
                    break;
            }

            if (oidcUpSequenceData.MaxAge.HasValue)
            {
                authenticationRequest.MaxAge = oidcUpSequenceData.MaxAge;
            }

            if (!oidcUpSequenceData.UserId.IsNullOrEmpty())
            {
                authenticationRequest.LoginHint = oidcUpSequenceData.UserId;
            }

            authenticationRequest.Scope = new[] { IdentityConstants.DefaultOidcScopes.OpenId}.ConcatOnce(party.Client.Scopes).ToSpaceList();

            //TODO add AcrValues
            //authenticationRequest.AcrValues = "urn:federation:authentication:windows";

            var nameValueCollection = authenticationRequest.ToDictionary();

            if (party.Client.EnablePkce)
            {
                var codeChallengeRequest = new CodeChallengeSecret
                {
                    CodeChallenge = await oidcUpSequenceData.CodeVerifier.Sha256HashBase64urlEncoded(),
                    CodeChallengeMethod = IdentityConstants.CodeChallengeMethods.S256,
                };
                logger.ScopeTrace($"CodeChallengeSecret request '{codeChallengeRequest.ToJsonIndented()}'.");

                nameValueCollection = nameValueCollection.AddToDictionary(codeChallengeRequest);
            }

            formActionLogic.AddFormActionAllowAll();

            logger.ScopeTrace($"Authentication request URL '{party.Client.AuthorizeUrl}'.");
            logger.ScopeTrace("Up, Sending OIDC Authentication request.", triggerEvent: true);
            return await nameValueCollection.ToRedirectResultAsync(party.Client.AuthorizeUrl);            
        }

        public async Task<IActionResult> AuthenticationResponseAsync(string partyId)
        {
            logger.ScopeTrace($"Up, OIDC Authentication response.");
            logger.SetScopeProperty("upPartyId", partyId);

            var party = await tenantRepository.GetAsync<OidcUpParty>(partyId);
            logger.SetScopeProperty("upPartyClientId", party.Client.ClientId);

            var formOrQueryDictionary = HttpContext.Request.Method switch
            {
                "POST" => HttpContext.Request.Form.ToDictionary(),
                "GET" => HttpContext.Request.Query.ToDictionary(),
                _ => throw new NotSupportedException($"Not supported response mode '{party.Client.ResponseMode}'")
            };

            var authenticationResponse = formOrQueryDictionary.ToObject<AuthenticationResponse>();
            logger.ScopeTrace($"Authentication response '{authenticationResponse.ToJsonIndented()}'.");
            if (authenticationResponse.State.IsNullOrEmpty()) throw new ArgumentNullException(nameof(authenticationResponse.State), authenticationResponse.GetTypeName());

            await sequenceLogic.ValidateSequenceAsync(authenticationResponse.State);
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<OidcUpSequenceData>(remove: true);

            var sessionResponse = formOrQueryDictionary.ToObject<SessionResponse>();
            if (sessionResponse != null)
            {
                logger.ScopeTrace($"Session response '{sessionResponse.ToJsonIndented()}'.");
            }

            try
            {
                logger.ScopeTrace("Up, OIDC Authentication response.", triggerEvent: true);

                bool isImplicitFlow = !party.Client.ResponseType.Contains(IdentityConstants.ResponseTypes.Code);
                ValidateAuthenticationResponse(party, authenticationResponse, sessionResponse, isImplicitFlow);

                (var claims, var idToken) = isImplicitFlow switch
                {
                    true => await ValidateTokensAsync(party, sequenceData, authenticationResponse.IdToken, authenticationResponse.AccessToken, true),
                    false => await HandleAuthorizationCodeResponseAsync(party, sequenceData, authenticationResponse.Code)
                };

                logger.ScopeTrace("Up, Successful OIDC Authentication response.", triggerEvent: true);

                var externalSessionId = sessionResponse?.SessionState.IsNullOrEmpty() switch
                {
                    false => sessionResponse.SessionState,
                    true => claims.FindFirstValue(c => c.Type == JwtClaimTypes.SessionId)
                };
                externalSessionId.ValidateMaxLength(IdentityConstants.MessageLength.SessionStatedMax, nameof(externalSessionId), "Session state or claim");
                claims = claims.Where(c => c.Type != JwtClaimTypes.SessionId).ToList();                

                var transformedClaims = await claimTransformationsLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
                var validClaims = ValidateClaims(party, transformedClaims);

                await sessionUpPartyLogic.CreateOrUpdateSessionAsync(party, GetDownPartyLink(party, sequenceData), validClaims, externalSessionId, idToken);

                return await AuthenticationResponseDownAsync(sequenceData, claims: validClaims);
            }
            catch (StopSequenceException)
            {
                throw;
            }
            catch (OAuthRequestException orex)
            {
                logger.SetScopeProperty("upPartyStatus", orex.Error);
                logger.Error(orex);
                return await AuthenticationResponseDownAsync(sequenceData, error: orex.Error, errorDescription: orex.ErrorDescription);
            }
            catch (ResponseErrorException rex)
            {
                logger.SetScopeProperty("upPartyStatus", rex.Error);
                logger.Error(rex);
                return await AuthenticationResponseDownAsync(sequenceData, error: rex.Error, errorDescription: $"{party.Name}|{rex.Message}");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return await AuthenticationResponseDownAsync(sequenceData, error: IdentityConstants.ResponseErrors.InvalidRequest);
            }
        }

        private DownPartyLink GetDownPartyLink(UpParty upParty, OidcUpSequenceData sequenceData) => upParty.DisableSingleLogout || sequenceData.DownPartyId == null || sequenceData.DownPartyType == null ? 
            null : new DownPartyLink { Id = sequenceData.DownPartyId, Type = sequenceData.DownPartyType.Value };

        private void ValidateAuthenticationResponse(OidcUpParty party, AuthenticationResponse authenticationResponse, SessionResponse sessionResponse, bool isImplicitFlow)
        {
            authenticationResponse.Validate(isImplicitFlow);

            if (party.Client.EnablePkce && isImplicitFlow)
            {
                throw new OAuthRequestException($"Require '{IdentityConstants.ResponseTypes.Code}' flow with PKCE.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
            }

            if (sessionResponse?.SessionState.IsNullOrEmpty() == false)
            {
                sessionResponse.Validate();
            }
        }

        private List<Claim> ValidateClaims(OidcUpParty party, List<Claim> claims)
        {
            var acceptedClaims = Constants.DefaultClaims.JwtTokenUpParty.ConcatOnce(party.Client.Claims).Where(c => !Constants.DefaultClaims.ExcludeJwtTokenUpParty.Contains(c));
            claims = claims.Where(c => acceptedClaims.Any(ic => ic == c.Type)).ToList();
            foreach (var claim in claims)
            {
                if (claim.Type?.Length > Constants.Models.Claim.JwtTypeLength)
                {
                    throw new OAuthRequestException($"Claim '{claim.Type.Substring(0, Constants.Models.Claim.JwtTypeLength)}' is too long, maximum length of '{Constants.Models.Claim.JwtTypeLength}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
                }

                if(Constants.EmbeddedJwtToken.JwtTokenClaims.Contains(claim.Type))
                {
                    if (claim.Value?.Length > Constants.EmbeddedJwtToken.ValueLength)
                    {
                        throw new OAuthRequestException($"Claim '{claim.Type}' value is too long, maximum length of '{Constants.EmbeddedJwtToken.ValueLength}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
                    }
                }
                else
                {
                    if (claim.Value?.Length > Constants.Models.Claim.ValueLength)
                    {
                        throw new OAuthRequestException($"Claim '{claim.Type}' value is too long, maximum length of '{Constants.Models.Claim.ValueLength}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
                    }
                }
            }
            return claims;
        }

        private async Task<(List<Claim>, string)> HandleAuthorizationCodeResponseAsync(OidcUpParty party, OidcUpSequenceData sequenceData, string code)
        {
            var tokenResponse = await TokenRequestAsync(party.Client, code, sequenceData);
            return await ValidateTokensAsync(party, sequenceData, tokenResponse.IdToken, tokenResponse.AccessToken, false);
        }

        private async Task<TokenResponse> TokenRequestAsync(OidcUpClient client, string code, OidcUpSequenceData sequenceData)
        {
            var tokenRequest = new TokenRequest
            {
                GrantType = IdentityConstants.GrantTypes.AuthorizationCode,
                Code = code,
                ClientId = !client.SpClientId.IsNullOrWhiteSpace() ? client.SpClientId : client.ClientId,
                RedirectUri = sequenceData.RedirectUri,
            };
            logger.ScopeTrace($"Token request '{tokenRequest.ToJsonIndented()}'.");
            var requestDictionary = tokenRequest.ToDictionary();

            if (!client.ClientSecret.IsNullOrEmpty())
            {
                var clientCredentials = new ClientCredentials
                {
                    ClientSecret = client.ClientSecret,
                };
                logger.ScopeTrace($"client credentials '{new ClientCredentials { ClientSecret = $"{(clientCredentials.ClientSecret?.Length > 10 ? clientCredentials.ClientSecret.Substring(0, 3) : string.Empty)}..." }.ToJsonIndented()}'.");
                requestDictionary = requestDictionary.AddToDictionary(clientCredentials);
            }

            if (client.EnablePkce)
            {
                var codeVerifierSecret = new CodeVerifierSecret
                {
                    CodeVerifier = sequenceData.CodeVerifier,
                };
                logger.ScopeTrace($"Code verifier secret '{codeVerifierSecret.ToJsonIndented()}'.");
                requestDictionary = requestDictionary.AddToDictionary(codeVerifierSecret);
            }

            var request = new HttpRequestMessage(HttpMethod.Post, client.TokenUrl);
            request.Content = new FormUrlEncodedContent(requestDictionary);

            var response = await httpClientFactory.CreateClient().SendAsync(request);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var result = await response.Content.ReadAsStringAsync();
                    var tokenResponse = result.ToObject<TokenResponse>();
                    logger.ScopeTrace($"Token response '{tokenResponse.ToJsonIndented()}'.");
                    tokenResponse.Validate(true);
                    if (tokenResponse.AccessToken.IsNullOrEmpty()) throw new ArgumentNullException(nameof(tokenResponse.AccessToken), tokenResponse.GetTypeName());
                    if (tokenResponse.ExpiresIn <= 0) throw new ArgumentNullException(nameof(tokenResponse.ExpiresIn), tokenResponse.GetTypeName());
                    return tokenResponse;

                case HttpStatusCode.BadRequest:
                    var resultBadRequest = await response.Content.ReadAsStringAsync();
                    var tokenResponseBadRequest = resultBadRequest.ToObject<TokenResponse>();
                    logger.ScopeTrace($"Bad token response '{tokenResponseBadRequest.ToJsonIndented()}'.");
                    tokenResponseBadRequest.Validate(true);
                    throw new EndpointException($"Bad request. Status code '{response.StatusCode}'. Response '{resultBadRequest}'.") { RouteBinding = RouteBinding };

                default:
                    throw new EndpointException($"Unexpected status code. Status code={response.StatusCode}") { RouteBinding = RouteBinding };
            }
        }

        private async Task<(List<Claim>, string)> ValidateTokensAsync(OidcUpParty party, OidcUpSequenceData sequenceData, string idToken, string accessToken, bool authorizationEndpoint)
        {
            var claims = await ValidateIdTokenAsync(party, sequenceData, idToken, accessToken, authorizationEndpoint);
            if (!accessToken.IsNullOrWhiteSpace())
            {
                if (!party.Client.UseIdTokenClaims)
                {
                    // If access token exists, use access token claims instead of ID token claims.
                    claims = ValidateAccessToken(party, sequenceData, accessToken);
                }
                claims.AddClaim(Constants.JwtClaimTypes.AccessToken, $"{party.Name}|{accessToken}");
            }

            var subject = claims.FindFirstValue(c => c.Type == JwtClaimTypes.Subject);
            if (!subject.IsNullOrEmpty())
            {
                claims = claims.Where(c => c.Type != JwtClaimTypes.Subject).ToList();
                claims.Add(new Claim(JwtClaimTypes.Subject, $"{party.Name}|{subject}"));
            }

            return await Task.FromResult((claims, idToken));
        }


        private async Task<List<Claim>> ValidateIdTokenAsync(OidcUpParty party, OidcUpSequenceData sequenceData, string idToken, string accessToken, bool authorizationEndpoint)
        {
            try
            {
                var jwtToken = JwtHandler.ReadToken(idToken);
                var issuer = party.Issuers.Where(i => i == jwtToken.Issuer).FirstOrDefault();
                if (string.IsNullOrEmpty(issuer))
                {
                    throw new OAuthRequestException($"{party.Name}|Id token issuer '{jwtToken.Issuer}' is unknown.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
                }

                (var claimsPrincipal, _) = JwtHandler.ValidateToken(idToken, issuer, party.Keys, sequenceData.ClientId);

                var nonce = claimsPrincipal.Claims.FindFirstValue(c => c.Type == JwtClaimTypes.Nonce);
                if (!sequenceData.Nonce.Equals(nonce, StringComparison.Ordinal))
                {
                    throw new OAuthRequestException($"{party.Name}|Id token nonce do not match.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
                }

                if (authorizationEndpoint && !accessToken.IsNullOrEmpty())
                {
                    var atHash = claimsPrincipal.Claims.FindFirstValue(c => c.Type == JwtClaimTypes.AtHash);
                    string algorithm = IdentityConstants.Algorithms.Asymmetric.RS256;
                    if (atHash != await accessToken.LeftMostBase64urlEncodedHash(algorithm))
                    {
                        throw new OAuthRequestException($"{party.Name}|Access Token hash claim in ID token do not match the access token.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
                    }
                }

                return claimsPrincipal.Claims.ToList();
            }
            catch (OAuthRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new OAuthRequestException($"{party.Name}|Id token not valid.", ex) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
            }
        }

        private List<Claim> ValidateAccessToken(OidcUpParty party, OidcUpSequenceData sequenceData, string accessToken)
        {
            try
            {
                var jwtToken = JwtHandler.ReadToken(accessToken);
                var issuer = party.Issuers.Where(i => i == jwtToken.Issuer).FirstOrDefault();
                if (string.IsNullOrEmpty(issuer))
                {
                    throw new OAuthRequestException($"{party.Name}|Access token issuer '{jwtToken.Issuer}' is unknown.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
                }


                (var claimsPrincipal, _) = JwtHandler.ValidateToken(accessToken, issuer, party.Keys, sequenceData.ClientId, validateAudience: false);
                return claimsPrincipal.Claims.ToList();
            }
            catch (OAuthRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new OAuthRequestException($"{party.Name}|Access token not valid.", ex) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
            }
        }

        private async Task<IActionResult> AuthenticationResponseDownAsync(OidcUpSequenceData sequenceData, List<Claim> claims = null, string error = null, string errorDescription = null)
        {
            try
            {
                logger.ScopeTrace($"Response, Down type {sequenceData.DownPartyType}.");
                switch (sequenceData.DownPartyType)
                {
                    case PartyTypes.OAuth2:
                        throw new NotImplementedException();
                    case PartyTypes.Oidc:
                        if (error.IsNullOrEmpty())
                        {
                            return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseAsync(sequenceData.DownPartyId, claims);
                        }
                        else
                        {
                            return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseErrorAsync(sequenceData.DownPartyId, error, errorDescription);
                        }
                    case PartyTypes.Saml2:
                        var claimsLogic = serviceProvider.GetService<ClaimsDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim>>();
                        return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequenceData.DownPartyId, ErrorToSamlStatus(error), claims != null ? await claimsLogic.FromJwtToSamlClaimsAsync(claims) : null);

                    default:
                        throw new NotSupportedException();
                }

            }
            catch (Exception ex)
            {
                throw new StopSequenceException("Falling authentication response down", ex);
            }
        }

        private Saml2StatusCodes ErrorToSamlStatus(string error)
        {
            if (error.IsNullOrEmpty())
            {
                return Saml2StatusCodes.Success;
            }

            switch (error)
            {
                case IdentityConstants.ResponseErrors.LoginRequired:
                    return Saml2StatusCodes.NoAuthnContext;

                default:
                    return Saml2StatusCodes.Responder;
            }
        }
    }
}
