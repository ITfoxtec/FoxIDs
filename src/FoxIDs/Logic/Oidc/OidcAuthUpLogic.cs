﻿using FoxIDs.Infrastructure;
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
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OidcAuthUpLogic<TParty, TClient> : OAuthAuthUpLogic<TParty, TClient> where TParty : OidcUpParty<TClient> where TClient : OidcUpClient
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly OidcJwtUpLogic<TParty, TClient> oidcJwtUpLogic;
        private readonly SequenceLogic sequenceLogic;
        private readonly PlanUsageLogic planUsageLogic;
        private readonly HrdLogic hrdLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly OidcDiscoveryReadUpLogic<TParty, TClient> oidcDiscoveryReadUpLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly StateUpPartyLogic stateUpPartyLogic;
        private readonly ExtendedUiLogic extendedUiLogic;
        private readonly ExternalUserLogic externalUserLogic;
        private readonly ClaimValidationLogic claimValidationLogic;
        private readonly IHttpClientFactory httpClientFactory;

        public OidcAuthUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantDataRepository tenantDataRepository, TrackIssuerLogic trackIssuerLogic, OidcJwtUpLogic<TParty, TClient> oidcJwtUpLogic, SequenceLogic sequenceLogic, PlanUsageLogic planUsageLogic, HrdLogic hrdLogic, SessionUpPartyLogic sessionUpPartyLogic, SecurityHeaderLogic securityHeaderLogic, OidcDiscoveryReadUpLogic<TParty, TClient> oidcDiscoveryReadUpLogic, ClaimTransformLogic claimTransformLogic, StateUpPartyLogic stateUpPartyLogic, ExtendedUiLogic extendedUiLogic, ExternalUserLogic externalUserLogic, ClaimValidationLogic claimValidationLogic, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor) : base(logger, tenantDataRepository, trackIssuerLogic, oidcJwtUpLogic, claimTransformLogic, claimValidationLogic, httpClientFactory, httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.oidcJwtUpLogic = oidcJwtUpLogic;
            this.sequenceLogic = sequenceLogic;
            this.planUsageLogic = planUsageLogic;
            this.hrdLogic = hrdLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.oidcDiscoveryReadUpLogic = oidcDiscoveryReadUpLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.stateUpPartyLogic = stateUpPartyLogic;
            this.extendedUiLogic = extendedUiLogic;
            this.externalUserLogic = externalUserLogic;
            this.claimValidationLogic = claimValidationLogic;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> AuthenticationRequestRedirectAsync(UpPartyLink partyLink, ILoginRequest loginRequest, string hrdLoginUpPartyName = null)
        {
            logger.ScopeTrace(() => "AuthMethod, OIDC Authentication request redirect.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            planUsageLogic.LogLoginEvent(PartyTypes.Oidc);

            await loginRequest.ValidateObjectAsync();

            var party = await tenantDataRepository.GetAsync<TParty>(partyId);

            var oidcUpSequenceData = new OidcUpSequenceData(loginRequest)
            {
                HrdLoginUpPartyName = hrdLoginUpPartyName,
                UpPartyId = partyId,
                UpPartyProfileName = partyLink.ProfileName
            };
            await sequenceLogic.SaveSequenceDataAsync(oidcUpSequenceData, partyName: party.Name);

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.OAuthUpJumpController, Constants.Endpoints.UpJump.AuthorizationRequest, includeSequence: true, partyBindingPattern: party.PartyBindingPattern).ToRedirectResult();
        }

        public async Task<IActionResult> AuthenticationRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "AuthMethod, OIDC Authentication request.");
            var oidcUpSequenceData = await sequenceLogic.GetSequenceDataAsync<OidcUpSequenceData>(partyName: partyId.PartyIdToName(), remove: false);
            if (!oidcUpSequenceData.UpPartyId.Equals(partyId, StringComparison.Ordinal))
            {
                throw new Exception("Invalid authentication method id.");
            }
            logger.SetScopeProperty(Constants.Logs.UpPartyId, oidcUpSequenceData.UpPartyId);

            var party = await tenantDataRepository.GetAsync<TParty>(oidcUpSequenceData.UpPartyId);
            logger.SetScopeProperty(Constants.Logs.UpPartyClientId, party.Client.ClientId);

            party = await oidcDiscoveryReadUpLogic.CheckOidcDiscoveryAndUpdatePartyAsync(party);

            var nonce = RandomGenerator.GenerateNonce();
            var loginCallBackUrl = HttpContext.GetUpPartyUrl(party.Name, Constants.Routes.OAuthController, Constants.Endpoints.AuthorizationResponse, partyBindingPattern: party.PartyBindingPattern);

            oidcUpSequenceData.ClientId = ResolveClientId(party);
            oidcUpSequenceData.RedirectUri = loginCallBackUrl;
            oidcUpSequenceData.Nonce = nonce;  
            if (party.Client.EnablePkce)
            {
                var codeVerifier = RandomGenerator.Generate(64);
                oidcUpSequenceData.CodeVerifier = codeVerifier;
            }
            await sequenceLogic.SaveSequenceDataAsync(oidcUpSequenceData, partyName: party.Name);

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
                case LoginAction.SessionUserRequireLogin:
                    authenticationRequest.Prompt = IdentityConstants.AuthorizationServerPrompt.Login;
                    break;
                case LoginAction.RequireLogin:
                    authenticationRequest.Prompt = IdentityConstants.AuthorizationServerPrompt.SelectAccount;
                    break;
                default:
                    break;
            }

            if (oidcUpSequenceData.MaxAge.HasValue)
            {
                authenticationRequest.MaxAge = oidcUpSequenceData.MaxAge;
            }

            if (!oidcUpSequenceData.LoginHint.IsNullOrEmpty())
            {
                authenticationRequest.LoginHint = oidcUpSequenceData.LoginHint;
            }

            var profile = GetProfile(party, oidcUpSequenceData);

            var scopes = new[] { IdentityConstants.DefaultOidcScopes.OpenId }.ConcatOnce(party.Client.Scopes);
            if (profile != null && profile.Client.Scopes?.Count() > 0)
            {
                scopes = scopes.ConcatOnce(profile.Client.Scopes);
            }
            authenticationRequest.Scope = scopes.ToSpaceList();

            //TODO add AcrValues
            //authenticationRequest.AcrValues = "urn:federation:authentication:windows";

            logger.ScopeTrace(() => $"AuthMethod, Authentication request '{authenticationRequest.ToJson()}'.", traceType: TraceTypes.Message);
            var nameValueCollection = authenticationRequest.ToDictionary();

            var additionalParameters = new List<OAuthAdditionalParameter>();
            if (party.Client.AdditionalParameters?.Count() > 0)
            {
                foreach (var additionalParameter in party.Client.AdditionalParameters)
                {
                    additionalParameters.Add(additionalParameter);
                }
            }
            if (profile != null && profile.Client.AdditionalParameters?.Count() > 0)
            {
                foreach (var additionalParameter in profile.Client.AdditionalParameters)
                {
                    var item = additionalParameters.Where(a => a.Name == additionalParameter.Name).FirstOrDefault();
                    if (item != null)
                    {
                        item.Value = additionalParameter.Value;
                    }
                    else
                    {
                        additionalParameters.Add(additionalParameter);
                    }
                }
            }

            if (additionalParameters.Count() > 0)
            {
                foreach (var additionalParameter in additionalParameters)
                {
                    if (!nameValueCollection.ContainsKey(additionalParameter.Name))
                    {
                        nameValueCollection.Add(additionalParameter.Name, additionalParameter.Value);
                    }                        
                }
                logger.ScopeTrace(() => $"AuthMethod, AdditionalParameters request '{{{string.Join(", ", additionalParameters.Select(p => $"\"{p.Name}\": \"{p.Value}\""))}}}'.", traceType: TraceTypes.Message);
            }

            if (party.Client.EnablePkce)
            {
                var codeChallengeRequest = new CodeChallengeSecret
                {
                    CodeChallenge = await oidcUpSequenceData.CodeVerifier.Sha256HashBase64urlEncodedAsync(),
                    CodeChallengeMethod = IdentityConstants.CodeChallengeMethods.S256,
                };

                logger.ScopeTrace(() => $"AuthMethod, CodeChallengeSecret request '{codeChallengeRequest.ToJson()}'.", traceType: TraceTypes.Message);
                nameValueCollection = nameValueCollection.AddToDictionary(codeChallengeRequest);
            }

            securityHeaderLogic.AddFormActionAllowAll();

            await stateUpPartyLogic.CreateOrUpdateStateCookieAsync(party, authenticationRequest.State);

            logger.ScopeTrace(() => $"AuthMethod, Authentication request URL '{party.Client.AuthorizeUrl}'.");
            logger.ScopeTrace(() => "AuthMethod, Sending OIDC Authentication request.", triggerEvent: true);
            return party.Client.AuthorizeUrl.ToRedirectResult(nameValueCollection);
        }

        private OAuthUpPartyProfile GetProfile(TParty party, OidcUpSequenceData oidcUpSequenceData)
        {
            if (!oidcUpSequenceData.UpPartyProfileName.IsNullOrEmpty() && party.Profiles != null)
            {
                return party.Profiles.Where(p => p.Name == oidcUpSequenceData.UpPartyProfileName).FirstOrDefault();
            }
            return null;
        }

        public async Task<IActionResult> AuthenticationResponseAsync(string partyId)
        {
            logger.ScopeTrace(() => $"AuthMethod, OIDC Authentication response.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            var party = await tenantDataRepository.GetAsync<TParty>(partyId);
            logger.SetScopeProperty(Constants.Logs.UpPartyClientId, party.Client.ClientId);

            (var formOrQueryDictionary, var onlyAcceptGetResponseWithError) = GetFormOrQueryDictionary(party);

            var authenticationResponse = formOrQueryDictionary.ToObject<AuthenticationResponse>();
            logger.ScopeTrace(() => $"AuthMethod, Authentication response '{authenticationResponse.ToJson()}'.", traceType: TraceTypes.Message);

            if (authenticationResponse.State.IsNullOrWhiteSpace())
            {
                authenticationResponse.State = await stateUpPartyLogic.GetAndDeleteStateCookieAsync(party);
            }
            else
            {
                await stateUpPartyLogic.DeleteStateCookieAsync(party);
            }

            try
            {
                await sequenceLogic.ValidateExternalSequenceIdAsync(authenticationResponse.State);
            }
            catch (Exception ex)
            {
                throw new Exception($"Invalid State '{authenticationResponse.State}' returned from the IdP.", ex);
            }

            var sequenceData = await sequenceLogic.GetSequenceDataAsync<OidcUpSequenceData>(partyName: party.Name, remove: false);

            if (party.Client.ClientKeys?.Where(ck => ck.Type == ClientKeyTypes.KeyVaultImport).Count() > 0)
            {
                await serviceProvider.GetService<ExternalKeyLogic>().PhasedOutExternalClientKeyAsync<TParty, TClient>(party);
            }

            var sessionResponse = formOrQueryDictionary.ToObject<SessionResponse>();
            if (sessionResponse != null)
            {
                logger.ScopeTrace(() => $"AuthMethod, Session response '{sessionResponse.ToJson()}'.", traceType: TraceTypes.Message);
            }

            try
            {
                logger.ScopeTrace(() => "AuthMethod, OIDC Authentication response.", triggerEvent: true);

                bool isImplicitFlow = !party.Client.ResponseType.Contains(IdentityConstants.ResponseTypes.Code);
                ValidateAuthenticationResponse(party, authenticationResponse, sessionResponse, isImplicitFlow, onlyAcceptGetResponseWithError);

                (var claims, var idToken, List<string> tokenIssuers) = isImplicitFlow switch
                {
                    true => await ValidateTokensAsync(party, sequenceData, authenticationResponse.IdToken, authenticationResponse.AccessToken, true),
                    false => await HandleAuthorizationCodeResponseAsync(party, sequenceData, authenticationResponse.Code)
                };
                logger.ScopeTrace(() => "AuthMethod, Successful OIDC Authentication response.", triggerEvent: true);
                logger.ScopeTrace(() => $"AuthMethod, OIDC received JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);

                var externalSessionId = claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.SessionId);
                externalSessionId.ValidateMaxLength(IdentityConstants.MessageLength.SessionIdMax, nameof(externalSessionId), "Session state or claim");
                claims = claims.Where(c => c.Type != JwtClaimTypes.SessionId &&
                    c.Type != Constants.JwtClaimTypes.AuthMethod && c.Type != Constants.JwtClaimTypes.AuthProfileMethod && c.Type != Constants.JwtClaimTypes.AuthMethodType &&
                    c.Type != Constants.JwtClaimTypes.UpParty && c.Type != Constants.JwtClaimTypes.UpPartyType &&
                    c.Type != Constants.JwtClaimTypes.AuthMethodIssuer).ToList();
                claims.AddClaim(Constants.JwtClaimTypes.AuthMethod, party.Name);
                if (!sequenceData.UpPartyProfileName.IsNullOrEmpty())
                {
                    claims.AddClaim(Constants.JwtClaimTypes.AuthProfileMethod, sequenceData.UpPartyProfileName);
                }
                claims.AddClaim(Constants.JwtClaimTypes.AuthMethodType, party.Type.GetPartyTypeValue());
                claims.AddClaim(Constants.JwtClaimTypes.UpParty, party.Name);
                claims.AddClaim(Constants.JwtClaimTypes.UpPartyType, party.Type.GetPartyTypeValue());
                if (tokenIssuers?.Count() > 0)
                {
                    foreach (var tokenIssuer in tokenIssuers)
                    {
                        claims.Add(new Claim(Constants.JwtClaimTypes.AuthMethodIssuer, $"{party.Name}|{tokenIssuer}"));
                    }
                }

                await sessionUpPartyLogic.CreateOrUpdateMarkerSessionAsync(party, sequenceData.DownPartyLink, externalSessionId, idToken: idToken);

                (var transformedClaims, var actionResult) = await claimTransformLogic.TransformAsync(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims, sequenceData);
                if (actionResult != null)
                {
                    await sequenceLogic.RemoveSequenceDataAsync<OidcUpSequenceData>(partyName: party.Name);
                    return actionResult;
                }

                var validClaims = claimValidationLogic.ValidateUpPartyClaims(party.Client.Claims, transformedClaims);
                logger.ScopeTrace(() => $"AuthMethod, OIDC transformed JWT claims '{validClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);

                var extendedUiActionResult = await HandleExtendedUiAsync(party, sequenceData, validClaims, externalSessionId, idToken);
                if (extendedUiActionResult != null)
                {
                    return extendedUiActionResult;
                }

                (var externalUserClaims, var externalUserActionResult) = await HandleExternalUserAsync(party, sequenceData, validClaims, externalSessionId, idToken);
                if (externalUserActionResult != null)
                {
                    return externalUserActionResult;
                }

                await sequenceLogic.RemoveSequenceDataAsync<OidcUpSequenceData>(partyName: party.Name);
                return await AuthenticationRequestPostAsync(party, sequenceData, validClaims, externalUserClaims, idToken, externalSessionId);
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

        private async Task<IActionResult> HandleExtendedUiAsync(TParty party, OidcUpSequenceData sequenceData, IEnumerable<Claim> claims, string externalSessionId, string idToken)
        {
            var extendedUiActionResult = await extendedUiLogic.HandleUiAsync(party, sequenceData, claims,
                (extendedUiUpSequenceData) =>
                {
                    extendedUiUpSequenceData.ExternalSessionId = externalSessionId;
                    extendedUiUpSequenceData.IdToken = idToken;
                });

            return extendedUiActionResult;
        }


        private async Task<(IEnumerable<Claim>, IActionResult)> HandleExternalUserAsync(TParty party, OidcUpSequenceData sequenceData, IEnumerable<Claim> claims, string externalSessionId, string idToken)
        {
            (var externalUserClaims, var externalUserActionResult, var deleteSequenceData) = await externalUserLogic.HandleUserAsync(party, sequenceData, claims,
                (externalUserUpSequenceData) =>
                {
                    externalUserUpSequenceData.ExternalSessionId = externalSessionId;
                    externalUserUpSequenceData.IdToken = idToken;
                },
                (errorMessage) => throw new OAuthRequestException(errorMessage) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest });
            if (externalUserActionResult != null)
            {
                if (deleteSequenceData)
                {
                    await sequenceLogic.RemoveSequenceDataAsync<OidcUpSequenceData>(partyName: party.Name);
                }
            }

            return (externalUserClaims, externalUserActionResult);
        }

        public async Task<IActionResult> AuthenticationRequestPostExtendedUiAsync(ExtendedUiUpSequenceData extendedUiSequenceData, IEnumerable<Claim> claims)
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<OidcUpSequenceData>(partyName: extendedUiSequenceData.UpPartyId.PartyIdToName(), remove: false);
            var party = await tenantDataRepository.GetAsync<TParty>(extendedUiSequenceData.UpPartyId);

            try
            {
                (var externalUserClaims, var externalUserActionResult) = await HandleExternalUserAsync(party, sequenceData, claims, extendedUiSequenceData.ExternalSessionId, extendedUiSequenceData.IdToken);
                if (externalUserActionResult != null)
                {
                    return externalUserActionResult;
                }

                await sequenceLogic.RemoveSequenceDataAsync<OidcUpSequenceData>(partyName: party.Name);
                return await AuthenticationRequestPostAsync(party, sequenceData, claims, externalUserClaims, extendedUiSequenceData.ExternalSessionId, extendedUiSequenceData.IdToken);
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

        public async Task<IActionResult> AuthenticationRequestPostExternalUserAsync(ExternalUserUpSequenceData externalUserSequenceData, IEnumerable<Claim> externalUserClaims)
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<OidcUpSequenceData>(partyName: externalUserSequenceData.UpPartyId.PartyIdToName(), remove: true);
            var party = await tenantDataRepository.GetAsync<TParty>(externalUserSequenceData.UpPartyId);

            try
            {
                return await AuthenticationRequestPostAsync(party, sequenceData, externalUserSequenceData.Claims?.ToClaimList(), externalUserClaims, externalUserSequenceData.ExternalSessionId, externalUserSequenceData.IdToken);
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

        private async Task<IActionResult> AuthenticationRequestPostAsync(TParty party, OidcUpSequenceData sequenceData, IEnumerable<Claim> validClaims, IEnumerable<Claim> externalUserClaims, string idToken, string externalSessionId)
        {
            validClaims = externalUserLogic.AddExternalUserClaims(party, validClaims, externalUserClaims);

            (var transformedClaims, var actionResult) = await claimTransformLogic.TransformAsync(party.ExitClaimTransforms?.ConvertAll(t => (ClaimTransform)t), validClaims, sequenceData);
            if (actionResult != null)
            {
                return actionResult;
            }

            var sessionId = await sessionUpPartyLogic.CreateOrUpdateSessionAsync(party, transformedClaims, externalSessionId, idToken);
            if (!sessionId.IsNullOrEmpty())
            {
                transformedClaims.AddOrReplaceClaim(JwtClaimTypes.SessionId, sessionId);
            }

            await hrdLogic.SaveHrdSelectionAsync(sequenceData.HrdLoginUpPartyName, sequenceData.UpPartyId.PartyIdToName(), sequenceData.UpPartyProfileName, PartyTypes.Oidc);

            logger.ScopeTrace(() => $"AuthMethod, OIDC output JWT claims '{transformedClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return await AuthenticationResponseDownAsync(sequenceData, claims: transformedClaims);
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

        private async Task<(List<Claim> claims, string idToken, List<string> tokenIssuers)> HandleAuthorizationCodeResponseAsync(TParty party, OidcUpSequenceData sequenceData, string code)
        {
            var tokenResponse = await TokenRequestAsync(party.Client, code, sequenceData);
            return await ValidateTokensAsync(party, sequenceData, tokenResponse.IdToken, tokenResponse.AccessToken, false);
        }

        private async Task<TokenResponse> TokenRequestAsync(TClient client, string code, OidcUpSequenceData sequenceData)
        {
            logger.ScopeTrace(() => $"AuthMethod, OIDC Token request URL '{client.TokenUrl}'.", traceType: TraceTypes.Message);
            var request = new HttpRequestMessage(HttpMethod.Post, client.TokenUrl);

            var tokenRequest = new TokenRequest
            {
                GrantType = IdentityConstants.GrantTypes.AuthorizationCode,
                Code = code,
                ClientId = !client.SpClientId.IsNullOrWhiteSpace() ? client.SpClientId : client.ClientId,
                RedirectUri = sequenceData.RedirectUri,
            };
            logger.ScopeTrace(() => $"AuthMethod, Token request '{tokenRequest.ToJson()}'.", traceType: TraceTypes.Message);
            var requestDictionary = tokenRequest.ToDictionary();

            requestDictionary = await AddClientAuthenticationAsync(client, tokenRequest.ClientId, request, requestDictionary);

            if (client.EnablePkce)
            {
                var codeVerifierSecret = new CodeVerifierSecret
                {
                    CodeVerifier = sequenceData.CodeVerifier,
                };
                logger.ScopeTrace(() => $"AuthMethod, Code verifier secret '{new CodeVerifierSecret { CodeVerifier = $"{(codeVerifierSecret.CodeVerifier?.Length > 10 ? codeVerifierSecret.CodeVerifier.Substring(0, 3) : string.Empty)}..." }.ToJson()}'.", traceType: TraceTypes.Message);
                requestDictionary = requestDictionary.AddToDictionary(codeVerifierSecret);
            }

            request.Content = new FormUrlEncodedContent(requestDictionary);

            using var response = await httpClientFactory.CreateClient().SendAsync(request);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var result = await response.Content.ReadAsStringAsync();
                    var tokenResponse = result.ToObject<TokenResponse>();
                    logger.ScopeTrace(() => $"AuthMethod, Token response '{tokenResponse.ToJson()}'.", traceType: TraceTypes.Message);
                    tokenResponse.Validate(true);
                    if (tokenResponse.AccessToken.IsNullOrEmpty()) throw new ArgumentNullException(nameof(tokenResponse.AccessToken), tokenResponse.GetTypeName());
                    if (tokenResponse.ExpiresIn <= 0) throw new ArgumentNullException(nameof(tokenResponse.ExpiresIn), tokenResponse.GetTypeName());
                    return tokenResponse;

                case HttpStatusCode.BadRequest:
                    var resultBadRequest = await response.Content.ReadAsStringAsync();
                    var tokenResponseBadRequest = resultBadRequest.ToObject<TokenResponse>();
                    logger.ScopeTrace(() => $"AuthMethod, Bad token response '{tokenResponseBadRequest.ToJson()}'.", traceType: TraceTypes.Message);
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
                        logger.ScopeTrace(() => $"AuthMethod, Unexpected status code token response '{tokenResponseUnexpectedStatus.ToJson()}'.", traceType: TraceTypes.Message);
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

        private async Task<Dictionary<string, string>> AddClientAuthenticationAsync(TClient client, string clientId, HttpRequestMessage request, Dictionary<string, string> requestDictionary)
        {
            if (client.ClientAuthenticationMethod == ClientAuthenticationMethods.ClientSecretBasic)
            {
                if (!client.ClientSecret.IsNullOrEmpty())
                {
                    logger.ScopeTrace(() => $"AuthMethod, Client credentials basic '{ new ClientCredentials { ClientSecret = $"{(client.ClientSecret?.Length > 10 ? client.ClientSecret.Substring(0, 3) : string.Empty)}..." }.ToJson() }'.", traceType: TraceTypes.Message);
                    request.Headers.Authorization = new AuthenticationHeaderValue(IdentityConstants.BasicAuthentication.Basic, $"{clientId.OAuthUrlDencode()}:{client.ClientSecret.OAuthUrlDencode()}".Base64Encode());
                }
            }
            else if (client.ClientAuthenticationMethod == ClientAuthenticationMethods.ClientSecretPost)
            {
                if (!client.ClientSecret.IsNullOrEmpty())
                {
                    var clientCredentials = new ClientCredentials
                    {
                        ClientSecret = client.ClientSecret,
                    };
                    logger.ScopeTrace(() => $"AuthMethod, Client credentials post '{ new ClientCredentials { ClientSecret = $"{(clientCredentials.ClientSecret?.Length > 10 ? clientCredentials.ClientSecret.Substring(0, 3) : string.Empty)}..." }.ToJson() }'.", traceType: TraceTypes.Message);
                    requestDictionary = requestDictionary.AddToDictionary(clientCredentials);
                }
            }
            else if (client.ClientAuthenticationMethod == ClientAuthenticationMethods.PrivateKeyJwt)
            {
                if (!(client.ClientKeys?.Count > 0))
                {
                    throw new ArgumentException($"Client id '{client.ClientId}' key is null.");
                }

                string algorithm = IdentityConstants.Algorithms.Asymmetric.RS256;
                var clientAssertionCredentials = new ClientAssertionCredentials
                {
                    ClientAssertionType = IdentityConstants.ClientAssertionTypes.JwtBearer,
                    ClientAssertion = await oidcJwtUpLogic.CreateClientAssertionAsync(client, clientId, algorithm)
                };
                logger.ScopeTrace(() => $"AuthMethod, Client credentials private key JWT '{new { client.ClientKeys.First().PublicKey.ToX509Certificate().Thumbprint }.ToJson()}'.", traceType: TraceTypes.Message);
                logger.ScopeTrace(() => $"AuthMethod, Client credentials assertion '{clientAssertionCredentials.ToJson()}'.", traceType: TraceTypes.Message);
                requestDictionary = requestDictionary.AddToDictionary(clientAssertionCredentials);
            }
            else
            {
                throw new NotImplementedException($"Client authentication method '{client.ClientAuthenticationMethod}' not implemented");
            }

            return requestDictionary;
        }

        protected async Task<(List<Claim> claims, string idToken, List<string> tokenIssuers)> ValidateTokensAsync(TParty party, OidcUpSequenceData sequenceData, string idToken, string accessToken, bool authorizationEndpoint)
        {
            var tokenIssuers = new List<string>();
            (var claims, var idTokenIssuer) = await ValidateIdTokenAsync(party, sequenceData, idToken, accessToken, authorizationEndpoint);
            if (!idTokenIssuer.IsNullOrEmpty())
            {
                tokenIssuers.Add(idTokenIssuer);
            }
            if (!accessToken.IsNullOrWhiteSpace())
            {
                if (party.Client.UseUserInfoClaims)
                {
                    claims = await UserInforRequestAsync(party.Client, accessToken);
                }
                else if (!party.Client.UseIdTokenClaims)
                {
                    var sessionIdClaim = claims.Where(c => c.Type == JwtClaimTypes.SessionId).FirstOrDefault();
                    (claims, var accessTokenIssuer) = await ValidateAccessTokenAsync(party, accessToken);
                    if (!accessTokenIssuer.IsNullOrEmpty())
                    {
                        tokenIssuers.Add(accessTokenIssuer);
                    }
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
                        claims.AddClaim(Constants.JwtClaimTypes.AccessToken, $"{party.Name}|{accessTokenClaim}");
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

            return (claims, idToken, tokenIssuers);
        }

        private async Task<(List<Claim> claims, string tokenIssuer)> ValidateIdTokenAsync(TParty party, OidcUpSequenceData sequenceData, string idToken, string accessToken, bool authorizationEndpoint)
        {
            try
            {
                var jwtToken = JwtHandler.ReadToken(idToken);
                (string issuer, string tokenIssuer) = ValidateIdTokenIssuer(party, jwtToken);

                var claimsPrincipal = await oidcJwtUpLogic.ValidateIdTokenAsync(idToken, issuer, party, sequenceData.ClientId);              
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

                return (claimsPrincipal.Claims.ToList(), !tokenIssuer.IsNullOrEmpty() ? $"id_token|{tokenIssuer}" : null);
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

        private (string issuer, string tokenIssuer) ValidateIdTokenIssuer(TParty party, JwtSecurityToken jwtToken)
        {
            var issuer = party.Issuers.Where(i => i == jwtToken.Issuer).FirstOrDefault();
            if (string.IsNullOrEmpty(issuer))
            {
                if (party.EditIssuersInAutomatic == true && party.Issuers.Where(i => i == "*").Any())
                {
                    return (null, jwtToken.Issuer);
                }
                else
                {
                    throw new OAuthRequestException($"{party.Name}|Id token issuer '{jwtToken.Issuer}' is unknown.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
                }
            }

            return (issuer, null);
        }

        private async Task<IActionResult> AuthenticationResponseDownAsync(OidcUpSequenceData sequenceData, List<Claim> claims = null, string error = null, string errorDescription = null)
        {
            try
            {
                logger.ScopeTrace(() => $"Response, Application type {sequenceData.DownPartyLink.Type}.");

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
                        return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequenceData.DownPartyLink.Id, SamlConvertLogic.ErrorToSamlStatus(error), jwtClaims: claims);
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
