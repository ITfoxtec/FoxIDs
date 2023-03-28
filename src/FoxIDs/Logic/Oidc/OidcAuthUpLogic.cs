using FoxIDs.Infrastructure;
using FoxIDs.Logic.Tracks;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
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
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OidcAuthUpLogic<TParty, TClient> : LogicSequenceBase where TParty : OidcUpParty<TClient> where TClient : OidcUpClient
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly JwtUpLogic<TParty, TClient> jwtUpLogic;
        private readonly SequenceLogic sequenceLogic;
        private readonly PlanUsageLogic planUsageLogic;
        private readonly HrdLogic hrdLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly OidcDiscoveryReadUpLogic<TParty, TClient> oidcDiscoveryReadUpLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly ClaimValidationLogic claimValidationLogic;
        private readonly IHttpClientFactory httpClientFactory;

        public OidcAuthUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, JwtUpLogic<TParty, TClient> jwtUpLogic, SequenceLogic sequenceLogic, PlanUsageLogic planUsageLogic, HrdLogic hrdLogic, SessionUpPartyLogic sessionUpPartyLogic, SecurityHeaderLogic securityHeaderLogic, OidcDiscoveryReadUpLogic<TParty, TClient> oidcDiscoveryReadUpLogic, ClaimTransformLogic claimTransformLogic, ClaimValidationLogic claimValidationLogic, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.jwtUpLogic = jwtUpLogic;
            this.sequenceLogic = sequenceLogic;
            this.planUsageLogic = planUsageLogic;
            this.hrdLogic = hrdLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.oidcDiscoveryReadUpLogic = oidcDiscoveryReadUpLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.claimValidationLogic = claimValidationLogic;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> AuthenticationRequestRedirectAsync(UpPartyLink partyLink, LoginRequest loginRequest, string hrdLoginUpPartyName = null)
        {
            logger.ScopeTrace(() => "Up, OIDC Authentication request redirect.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            await loginRequest.ValidateObjectAsync();

            var party = await tenantRepository.GetAsync<TParty>(partyId);

            var oidcUpSequenceData = new OidcUpSequenceData
            {
                DownPartyLink = loginRequest.DownPartyLink,
                HrdLoginUpPartyName = hrdLoginUpPartyName,
                UpPartyId = partyId,
                LoginAction = loginRequest.LoginAction,
                UserId = loginRequest.UserId,
                MaxAge = loginRequest.MaxAge,
                LoginEmailHint = loginRequest.EmailHint
            };
            await sequenceLogic.SaveSequenceDataAsync(oidcUpSequenceData);

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.OAuthUpJumpController, Constants.Endpoints.UpJump.AuthenticationRequest, includeSequence: true, partyBindingPattern: party.PartyBindingPattern).ToRedirectResult();
        }

        public async Task<IActionResult> AuthenticationRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "Up, OIDC Authentication request.");
            var oidcUpSequenceData = await sequenceLogic.GetSequenceDataAsync<OidcUpSequenceData>(remove: false);
            if (!oidcUpSequenceData.UpPartyId.Equals(partyId, StringComparison.Ordinal))
            {
                throw new Exception("Invalid up-party id.");
            }
            logger.SetScopeProperty(Constants.Logs.UpPartyId, oidcUpSequenceData.UpPartyId);

            var party = await tenantRepository.GetAsync<TParty>(oidcUpSequenceData.UpPartyId);
            logger.SetScopeProperty(Constants.Logs.UpPartyClientId, party.Client.ClientId);

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
                State = await sequenceLogic.CreateExternalSequenceIdAsync()
            };

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

            if (!oidcUpSequenceData.LoginEmailHint.IsNullOrEmpty())
            {
                authenticationRequest.LoginHint = oidcUpSequenceData.LoginEmailHint;
            }

            authenticationRequest.Scope = new[] { IdentityConstants.DefaultOidcScopes.OpenId}.ConcatOnce(party.Client.Scopes).ToSpaceList();

            //TODO add AcrValues
            //authenticationRequest.AcrValues = "urn:federation:authentication:windows";

            logger.ScopeTrace(() => $"Up, Authentication request '{authenticationRequest.ToJsonIndented()}'.", traceType: TraceTypes.Message);
            var nameValueCollection = authenticationRequest.ToDictionary();

            if (party.Client.EnablePkce)
            {
                var codeChallengeRequest = new CodeChallengeSecret
                {
                    CodeChallenge = await oidcUpSequenceData.CodeVerifier.Sha256HashBase64urlEncodedAsync(),
                    CodeChallengeMethod = IdentityConstants.CodeChallengeMethods.S256,
                };

                logger.ScopeTrace(() => $"Up, CodeChallengeSecret request '{codeChallengeRequest.ToJsonIndented()}'.", traceType: TraceTypes.Message);
                nameValueCollection = nameValueCollection.AddToDictionary(codeChallengeRequest);
            }

            securityHeaderLogic.AddFormActionAllowAll();

            logger.ScopeTrace(() => $"Up, Authentication request URL '{party.Client.AuthorizeUrl}'.");
            logger.ScopeTrace(() => "Up, Sending OIDC Authentication request.", triggerEvent: true);
            return await nameValueCollection.ToRedirectResultAsync(party.Client.AuthorizeUrl);            
        }

        public async Task<IActionResult> AuthenticationResponseAsync(string partyId)
        {
            logger.ScopeTrace(() => $"Up, OIDC Authentication response.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            var party = await tenantRepository.GetAsync<TParty>(partyId);
            logger.SetScopeProperty(Constants.Logs.UpPartyClientId, party.Client.ClientId);

            (var formOrQueryDictionary, var onlyAcceptGetResponseWithError) = GetFormOrQueryDictionary(party);

            var authenticationResponse = formOrQueryDictionary.ToObject<AuthenticationResponse>();
            logger.ScopeTrace(() => $"Up, Authentication response '{authenticationResponse.ToJsonIndented()}'.", traceType: TraceTypes.Message);
            if (authenticationResponse.State.IsNullOrEmpty()) throw new ArgumentNullException(nameof(authenticationResponse.State), authenticationResponse.GetTypeName());

            await sequenceLogic.ValidateExternalSequenceIdAsync(authenticationResponse.State);
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<OidcUpSequenceData>(remove: true);

            var sessionResponse = formOrQueryDictionary.ToObject<SessionResponse>();
            if (sessionResponse != null)
            {
                logger.ScopeTrace(() => $"Up, Session response '{sessionResponse.ToJsonIndented()}'.", traceType: TraceTypes.Message);
            }

            try
            {
                logger.ScopeTrace(() => "Up, OIDC Authentication response.", triggerEvent: true);

                bool isImplicitFlow = !party.Client.ResponseType.Contains(IdentityConstants.ResponseTypes.Code);
                ValidateAuthenticationResponse(party, authenticationResponse, sessionResponse, isImplicitFlow, onlyAcceptGetResponseWithError);

                (var claims, var idToken) = isImplicitFlow switch
                {
                    true => await ValidateTokensAsync(party, sequenceData, authenticationResponse.IdToken, authenticationResponse.AccessToken, true),
                    false => await HandleAuthorizationCodeResponseAsync(party, sequenceData, authenticationResponse.Code)
                };
                logger.ScopeTrace(() => "Up, Successful OIDC Authentication response.", triggerEvent: true);
                logger.ScopeTrace(() => $"Up, OIDC received JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);

                var externalSessionId = claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.SessionId);
                externalSessionId.ValidateMaxLength(IdentityConstants.MessageLength.SessionIdMax, nameof(externalSessionId), "Session state or claim");
                claims = claims.Where(c => c.Type != JwtClaimTypes.SessionId && c.Type != Constants.JwtClaimTypes.UpParty && c.Type != Constants.JwtClaimTypes.UpPartyType).ToList();
                claims.AddClaim(Constants.JwtClaimTypes.UpParty, party.Name);
                claims.AddClaim(Constants.JwtClaimTypes.UpPartyType, party.Type.ToString().ToLower());

                var transformedClaims = await claimTransformLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
                var validClaims = claimValidationLogic.ValidateUpPartyClaims(party.Client.Claims, transformedClaims);

                var sessionId = await sessionUpPartyLogic.CreateOrUpdateSessionAsync(party, party.DisableSingleLogout ? null : sequenceData.DownPartyLink, validClaims, externalSessionId, idToken);
                if (!sessionId.IsNullOrEmpty())
                {
                    validClaims.AddClaim(JwtClaimTypes.SessionId, sessionId);
                }

                if (!sequenceData.HrdLoginUpPartyName.IsNullOrEmpty())
                {
                    await hrdLogic.SaveHrdSelectionAsync(sequenceData.HrdLoginUpPartyName, sequenceData.UpPartyId.PartyIdToName(), PartyTypes.Oidc);
                }

                logger.ScopeTrace(() => $"Up, OIDC output JWT claims '{validClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
                return await AuthenticationResponseDownAsync(sequenceData, claims: validClaims);
            }
            catch (StopSequenceException)
            {
                throw;
            }
            catch (OAuthRequestException orex)
            {
                logger.SetScopeProperty(Constants.Logs.UpPartyStatus, orex.Error);
                logger.Error(orex);
                return await AuthenticationResponseDownAsync(sequenceData, error: orex.Error, errorDescription: orex.ErrorDescription);
            }
            catch (ResponseErrorException rex)
            {
                logger.SetScopeProperty(Constants.Logs.UpPartyStatus, rex.Error);
                logger.Error(rex);
                return await AuthenticationResponseDownAsync(sequenceData, error: rex.Error, errorDescription: $"{party.Name}|{rex.Message}");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return await AuthenticationResponseDownAsync(sequenceData, error: IdentityConstants.ResponseErrors.InvalidRequest);
            }
        }

        private (Dictionary<string, string> formOrQueryDictionary, bool onlyAcceptGetResponseWithError) GetFormOrQueryDictionary(TParty party)
        {
            switch (HttpContext.Request.Method)
            {
                case "POST":
                    if (party.Client.ResponseMode == IdentityConstants.ResponseModes.FormPost)
                    {
                        return (HttpContext.Request.Form.ToDictionary(), false);
                    }
                    throw new NotSupportedException($"POST not supported by response mode '{party.Client.ResponseMode}'.");

                case "GET":
                    var formOrQueryDictionary = HttpContext.Request.Query.ToDictionary();
                    if (party.Client.ResponseMode == IdentityConstants.ResponseModes.Query)
                    {
                        return (formOrQueryDictionary, false);
                    }
                    else
                    {
                        return (formOrQueryDictionary, true);
                    }

                default:
                    throw new NotSupportedException($"Request method not supported by response mode '{party.Client.ResponseMode}'");
            }
        }

        private void ValidateAuthenticationResponse(TParty party, AuthenticationResponse authenticationResponse, SessionResponse sessionResponse, bool isImplicitFlow, bool onlyAcceptGetResponseWithError)
        {
            authenticationResponse.Validate(isImplicitFlow);
            if (onlyAcceptGetResponseWithError)
            {
                throw new NotSupportedException($"GET not supported by response mode '{party.Client.ResponseMode}'.");
            }

            if (party.Client.EnablePkce && isImplicitFlow)
            {
                throw new OAuthRequestException($"Require '{IdentityConstants.ResponseTypes.Code}' flow with PKCE.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
            }

            if (sessionResponse?.SessionState.IsNullOrEmpty() == false)
            {
                sessionResponse.Validate();
            }
        }

        private async Task<(List<Claim>, string)> HandleAuthorizationCodeResponseAsync(TParty party, OidcUpSequenceData sequenceData, string code)
        {
            var tokenResponse = await TokenRequestAsync(party.Client, code, sequenceData);
            return await ValidateTokensAsync(party, sequenceData, tokenResponse.IdToken, tokenResponse.AccessToken, false);
        }

        private async Task<TokenResponse> TokenRequestAsync(TClient client, string code, OidcUpSequenceData sequenceData)
        {
            var tokenRequest = new TokenRequest
            {
                GrantType = IdentityConstants.GrantTypes.AuthorizationCode,
                Code = code,
                ClientId = !client.SpClientId.IsNullOrWhiteSpace() ? client.SpClientId : client.ClientId,
                RedirectUri = sequenceData.RedirectUri,
            };
            logger.ScopeTrace(() => $"Up, Token request '{tokenRequest.ToJsonIndented()}'.", traceType: TraceTypes.Message);
            var requestDictionary = tokenRequest.ToDictionary();

            if (!client.ClientSecret.IsNullOrEmpty())
            {
                var clientCredentials = new ClientCredentials
                {
                    ClientSecret = client.ClientSecret,
                };
                logger.ScopeTrace(() => $"Up, Client credentials '{new ClientCredentials { ClientSecret = $"{(clientCredentials.ClientSecret?.Length > 10 ? clientCredentials.ClientSecret.Substring(0, 3) : string.Empty)}..." }.ToJsonIndented()}'.", traceType: TraceTypes.Message);
                requestDictionary = requestDictionary.AddToDictionary(clientCredentials);
            }

            if (client.EnablePkce)
            {
                var codeVerifierSecret = new CodeVerifierSecret
                {
                    CodeVerifier = sequenceData.CodeVerifier,
                };
                logger.ScopeTrace(() => $"Up, Code verifier secret '{new CodeVerifierSecret { CodeVerifier = $"{(codeVerifierSecret.CodeVerifier?.Length > 10 ? codeVerifierSecret.CodeVerifier.Substring(0, 3) : string.Empty)}..." }.ToJsonIndented()}'.", traceType: TraceTypes.Message);
                requestDictionary = requestDictionary.AddToDictionary(codeVerifierSecret);
            }

            logger.ScopeTrace(() => $"Up, OIDC Token request URL '{client.TokenUrl}'.", traceType: TraceTypes.Message);
            var request = new HttpRequestMessage(HttpMethod.Post, client.TokenUrl);
            request.Content = new FormUrlEncodedContent(requestDictionary);

            using var response = await httpClientFactory.CreateClient(nameof(HttpClient)).SendAsync(request);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var result = await response.Content.ReadAsStringAsync();
                    var tokenResponse = result.ToObject<TokenResponse>();
                    logger.ScopeTrace(() => $"Up, Token response '{tokenResponse.ToJsonIndented()}'.", traceType: TraceTypes.Message);
                    tokenResponse.Validate(true);
                    if (tokenResponse.AccessToken.IsNullOrEmpty()) throw new ArgumentNullException(nameof(tokenResponse.AccessToken), tokenResponse.GetTypeName());
                    if (tokenResponse.ExpiresIn <= 0) throw new ArgumentNullException(nameof(tokenResponse.ExpiresIn), tokenResponse.GetTypeName());
                    return tokenResponse;

                case HttpStatusCode.BadRequest:
                    var resultBadRequest = await response.Content.ReadAsStringAsync();
                    var tokenResponseBadRequest = resultBadRequest.ToObject<TokenResponse>();
                    logger.ScopeTrace(() => $"Up, Bad token response '{tokenResponseBadRequest.ToJsonIndented()}'.", traceType: TraceTypes.Message);
                    try
                    {
                        tokenResponseBadRequest.Validate(true);
                    }
                    catch (ResponseErrorException rex)
                    {
                        throw new OAuthRequestException($"External {rex.Message}") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
                    }
                    throw new EndpointException($"Bad request. Status code '{response.StatusCode}'. Response '{resultBadRequest}'.") { RouteBinding = RouteBinding };

                default:
                    try
                    {
                        var resultUnexpectedStatus = await response.Content.ReadAsStringAsync();
                        var tokenResponseUnexpectedStatus = resultUnexpectedStatus.ToObject<TokenResponse>();
                        logger.ScopeTrace(() => $"Up, Unexpected status code token response '{tokenResponseUnexpectedStatus.ToJsonIndented()}'.", traceType: TraceTypes.Message);
                        try
                        {
                            tokenResponseUnexpectedStatus.Validate(true);
                        }
                        catch (ResponseErrorException rex)
                        {
                            throw new OAuthRequestException($"External {rex.Message}") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
                        }
                    }
                    catch (OAuthRequestException)
                    {
                        throw;
                    }
                    catch (Exception ex) 
                    {
                        throw new EndpointException($"Unexpected status code. Status code={response.StatusCode}", ex) { RouteBinding = RouteBinding };
                    }
                    throw new EndpointException($"Unexpected status code. Status code={response.StatusCode}") { RouteBinding = RouteBinding };
            }
        }

        private async Task<(List<Claim>, string)> ValidateTokensAsync(TParty party, OidcUpSequenceData sequenceData, string idToken, string accessToken, bool authorizationEndpoint)
        {
            var claims = await ValidateIdTokenAsync(party, sequenceData, idToken, accessToken, authorizationEndpoint);
            if (!accessToken.IsNullOrWhiteSpace())
            {
                if (party.Client.UseUserInfoClaims)
                {
                    claims = await UserInforRequestAsync(party.Client, accessToken);
                }
                else if (!party.Client.UseIdTokenClaims)
                {
                    var sessionIdClaim = claims.Where(c => c.Type == JwtClaimTypes.SessionId).FirstOrDefault();
                    claims = await ValidateAccessTokenAsync(party, sequenceData, accessToken);
                    if (sessionIdClaim != null && !claims.Where(c => c.Type == JwtClaimTypes.SessionId).Any())
                    {
                        claims.Add(sessionIdClaim);
                    }
                }

                var accessTokenClaims = claims.Where(c => c.Type == Constants.JwtClaimTypes.AccessToken).Select(c => c.Value);
                if (accessTokenClaims.Count() > 0)
                {
                    claims = claims.Where(c => c.Type != Constants.JwtClaimTypes.AccessToken).ToList();
                    foreach (var accessTokenClaim in accessTokenClaims)
                    {
                        claims.Add(new Claim(Constants.JwtClaimTypes.AccessToken, $"{party.Name}|{accessTokenClaim}"));
                    }
                }

                claims.AddClaim(Constants.JwtClaimTypes.AccessToken, $"{party.Name}|{accessToken}");
            }

            var subject = claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Subject);

            if (subject.IsNullOrEmpty())
            {
                subject = claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Email);
            }

            if (!subject.IsNullOrEmpty())
            {
                claims = claims.Where(c => c.Type != JwtClaimTypes.Subject).ToList();
                claims.Add(new Claim(JwtClaimTypes.Subject, $"{party.Name}|{subject}"));
            }

            return (claims, idToken);
        }

        private async Task<List<Claim>> ValidateIdTokenAsync(TParty party, OidcUpSequenceData sequenceData, string idToken, string accessToken, bool authorizationEndpoint)
        {
            try
            {
                var jwtToken = JwtHandler.ReadToken(idToken);
                var issuer = party.Issuers.Where(i => i == jwtToken.Issuer).FirstOrDefault();
                if (string.IsNullOrEmpty(issuer))
                {
                    throw new OAuthRequestException($"{party.Name}|Id token issuer '{jwtToken.Issuer}' is unknown.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
                }

                var claimsPrincipal = await jwtUpLogic.ValidateIdTokenAsync(idToken, issuer, party, sequenceData.ClientId);
                var nonce = claimsPrincipal.Claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Nonce);
                if (!sequenceData.Nonce.Equals(nonce, StringComparison.Ordinal))
                {
                    throw new OAuthRequestException($"{party.Name}|Id token nonce do not match.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
                }

                if (authorizationEndpoint && !accessToken.IsNullOrEmpty())
                {
                    var atHash = claimsPrincipal.Claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.AtHash);
                    string algorithm = IdentityConstants.Algorithms.Asymmetric.RS256;
                    if (atHash != await accessToken.LeftMostBase64urlEncodedHashAsync(algorithm))
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

        private async Task<List<Claim>> ValidateAccessTokenAsync(TParty party, OidcUpSequenceData sequenceData, string accessToken)
        {
            try
            {
                var jwtToken = JwtHandler.ReadToken(accessToken);
                var issuer = party.Issuers.Where(i => i == jwtToken.Issuer).FirstOrDefault();
                if (string.IsNullOrEmpty(issuer))
                {
                    throw new OAuthRequestException($"{party.Name}|Access token issuer '{jwtToken.Issuer}' is unknown.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
                }

                var claimsPrincipal = await jwtUpLogic.ValidateAccessTokenAsync(accessToken, issuer, party, sequenceData.ClientId);              
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

        private async Task<List<Claim>> UserInforRequestAsync(TClient client, string accessToken)
        {
            ValidateClientUserInfoSupport(client);
            logger.ScopeTrace(() => $"Up, OIDC UserInfo request URL '{client.UserInfoUrl}'.", traceType: TraceTypes.Message);
    
            var httpClient = httpClientFactory.CreateClient(nameof(HttpClient));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.TokenTypes.Bearer, accessToken);

            using var response = await httpClient.GetAsync(client.UserInfoUrl);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var result = await response.Content.ReadAsStringAsync();
                    var userInfoResponse = result.ToObject<Dictionary<string, string>>();
                    logger.ScopeTrace(() => $"Up, UserInfo response '{userInfoResponse.ToJsonIndented()}'.", traceType: TraceTypes.Message);

                    var claims = userInfoResponse.Select(c => new Claim(c.Key, c.Value)).ToList();
                    if (!claims.Any(c => c.Type == JwtClaimTypes.Subject))
                    {
                        throw new OAuthRequestException($"Require {JwtClaimTypes.Subject} claim from userinfo endpoint.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
                    }
                    return claims;

                case HttpStatusCode.BadRequest:
                    var resultBadRequest = await response.Content.ReadAsStringAsync();
                    var userInfoResponseBadRequest = resultBadRequest.ToObject<TokenResponse>();
                    logger.ScopeTrace(() => $"Up, Bad userinfo response '{userInfoResponseBadRequest.ToJsonIndented()}'.", traceType: TraceTypes.Message);
                    try
                    {
                        userInfoResponseBadRequest.Validate(true);
                    }
                    catch (ResponseErrorException rex)
                    {
                        throw new OAuthRequestException($"External {rex.Message}") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
                    }
                    throw new EndpointException($"Bad request. Status code '{response.StatusCode}'. Response '{resultBadRequest}'.") { RouteBinding = RouteBinding };

                default:
                    try
                    {
                        var resultUnexpectedStatus = await response.Content.ReadAsStringAsync();
                        var userInfoResponseUnexpectedStatus = resultUnexpectedStatus.ToObject<TokenResponse>();
                        logger.ScopeTrace(() => $"Up, Unexpected status code userinfo response '{userInfoResponseUnexpectedStatus.ToJsonIndented()}'.", traceType: TraceTypes.Message);
                        try
                        {
                            userInfoResponseUnexpectedStatus.Validate(true);
                        }
                        catch (ResponseErrorException rex)
                        {
                            throw new OAuthRequestException($"External {rex.Message}") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
                        }
                    }
                    catch (OAuthRequestException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new EndpointException($"Unexpected status code. Status code={response.StatusCode}", ex) { RouteBinding = RouteBinding };
                    }
                    throw new EndpointException($"Unexpected status code. Status code={response.StatusCode}") { RouteBinding = RouteBinding };
            }
        }

        private void ValidateClientUserInfoSupport(TClient client)
        {
            if (client.UserInfoUrl.IsNullOrEmpty())
            {
                throw new EndpointException("Userinfo endpoint not configured.") { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> AuthenticationResponseDownAsync(OidcUpSequenceData sequenceData, List<Claim> claims = null, string error = null, string errorDescription = null)
        {
            try
            {
                logger.ScopeTrace(() => $"Response, Down type {sequenceData.DownPartyLink.Type}.");

                if (error.IsNullOrEmpty())
                {
                    planUsageLogic.LogLoginEvent();
                }

                switch (sequenceData.DownPartyLink.Type)
                {
                    case PartyTypes.OAuth2:
                        throw new NotImplementedException();
                    case PartyTypes.Oidc:
                        if (error.IsNullOrEmpty())
                        {
                            return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseAsync(sequenceData.DownPartyLink.Id, claims);
                        }
                        else
                        {
                            return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseErrorAsync(sequenceData.DownPartyLink.Id, error, errorDescription);
                        }
                    case PartyTypes.Saml2:
                        var claimsLogic = serviceProvider.GetService<ClaimsOAuthDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim>>();
                        return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequenceData.DownPartyLink.Id, SamlConvertLogic.ErrorToSamlStatus(error), claims != null ? await claimsLogic.FromJwtToSamlClaimsAsync(claims) : null);
                    case PartyTypes.TrackLink:
                        return await serviceProvider.GetService<TrackLinkAuthDownLogic>().AuthResponseAsync(sequenceData.DownPartyLink.Id, claims, error, errorDescription);                        

                    default:
                        throw new NotSupportedException();
                }

            }
            catch (Exception ex)
            {
                throw new StopSequenceException("Falling authentication response down", ex);
            }
        }
    }
}
