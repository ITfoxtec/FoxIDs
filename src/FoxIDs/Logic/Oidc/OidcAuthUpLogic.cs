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
using Microsoft.AspNetCore.WebUtilities;
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
        private readonly OidcDiscoveryReadUpLogic oidcDiscoveryReadUpLogic;
        private readonly ClaimTransformationsLogic claimTransformationsLogic;
        private readonly IHttpClientFactory httpClientFactory;

        public OidcAuthUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, OidcDiscoveryReadUpLogic oidcDiscoveryReadUpLogic, ClaimTransformationsLogic claimTransformationsLogic, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.oidcDiscoveryReadUpLogic = oidcDiscoveryReadUpLogic;
            this.claimTransformationsLogic = claimTransformationsLogic;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> AuthenticationRequestAsync(UpPartyLink partyLink, LoginRequest loginRequest)
        {
            logger.ScopeTrace("Up, OIDC Authentication request.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty("upPartyId", partyId);

            await loginRequest.ValidateObjectAsync();

            var party = await tenantRepository.GetAsync<OidcUpParty>(partyId);
            logger.SetScopeProperty("upPartyClientId", party.Client.ClientId);

            await oidcDiscoveryReadUpLogic.CheckOidcDiscoveryAndUpdatePartyAsync(party);

            var nonce = RandomGenerator.GenerateNonce();
            var loginCallBackUrl = UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName, $"({party.Name})", Constants.Routes.OAuthController, Constants.Endpoints.AuthorizationResponse);

            var oidcUpSequenceData = new OidcUpSequenceData
            {
                DownPartyId = loginRequest.DownParty.Id,
                DownPartyType = loginRequest.DownParty.Type,
                ClientId = !party.Client.SpClientId.IsNullOrWhiteSpace() ? party.Client.SpClientId : party.Client.ClientId,
                RedirectUri = loginCallBackUrl,
                Nonce = nonce                
            };
            if (party.Client.RequirePkce)
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

            switch (loginRequest.LoginAction)
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

            if (loginRequest.MaxAge.HasValue)
            {
                authenticationRequest.MaxAge = loginRequest.MaxAge;
            }

            if (!loginRequest.UserId.IsNullOrEmpty())
            {
                authenticationRequest.LoginHint = loginRequest.UserId;
            }

            authenticationRequest.Scope = new[] { IdentityConstants.DefaultOidcScopes.OpenId}.ConcatOnce(party.Client.Scopes).ToSpaceList();

            //TODO add AcrValues
            //authenticationRequest.AcrValues = "urn:federation:authentication:windows";

            var requestDictionary = authenticationRequest.ToDictionary();

            if (party.Client.RequirePkce)
            {
                var codeChallengeRequest = new CodeChallengeSecret
                {
                    CodeChallenge = await oidcUpSequenceData.CodeVerifier.Sha256HashBase64urlEncoded(),
                    CodeChallengeMethod = IdentityConstants.CodeChallengeMethods.S256,
                };
                logger.ScopeTrace($"CodeChallengeSecret request '{codeChallengeRequest.ToJsonIndented()}'.");

                requestDictionary = requestDictionary.AddToDictionary(codeChallengeRequest);
            }

            var authenticationRequestUrl = QueryHelpers.AddQueryString(party.Client.AuthorizeUrl, requestDictionary);
            logger.ScopeTrace($"Authentication request URL '{authenticationRequestUrl}'.");
            logger.ScopeTrace("Up, Sending OIDC Authentication request.", triggerEvent: true);
            return new RedirectResult(authenticationRequestUrl);
        }

        public async Task<IActionResult> AuthenticationResponseAsync(string partyId)
        {
            logger.ScopeTrace($"Up, OIDC Authentication response.");
            logger.SetScopeProperty("upPartyId", partyId);

            var party = await tenantRepository.GetAsync<OidcUpParty>(partyId);
            logger.SetScopeProperty("upPartyClientId", party.Client.ClientId);

            var formOrQueryDictionary = party.Client.ResponseMode switch
            {
                IdentityConstants.ResponseModes.FormPost => HttpContext.Request.Form.ToDictionary(),
                IdentityConstants.ResponseModes.Query => HttpContext.Request.Query.ToDictionary(),
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

                var claims = isImplicitFlow switch
                {
                    true => await ValidateTokens(party, sequenceData, authenticationResponse.IdToken, authenticationResponse.AccessToken),
                    false => await HandleAuthorizationCodeResponseAsync(party, sequenceData, authenticationResponse.Code)
                };

                logger.ScopeTrace("Up, Successful OIDC Authentication response.", triggerEvent: true);

                if (sessionResponse?.SessionState.IsNullOrEmpty() == false)
                {
                    claims.AddClaim(JwtClaimTypes.SessionId, sessionResponse.SessionState);
                }

                var transformedClaims = await claimTransformationsLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
                var validClaims = ValidateClaims(party, transformedClaims);

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

        private void ValidateAuthenticationResponse(OidcUpParty party, AuthenticationResponse authenticationResponse, SessionResponse sessionResponse, bool isImplicitFlow)
        {
            authenticationResponse.Validate(isImplicitFlow);

            if (party.Client.RequirePkce && isImplicitFlow)
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
            var acceptedClaims = Constants.DefaultClaims.JwtTokenUpParty.ConcatOnce(party.Client.Claims).Where(c => !Constants.DefaultClaims.ExcludeJwtTokenUpParty.Any(ex => ex == c));
            claims = claims.Where(c => acceptedClaims.Any(ic => ic == c.Type)).ToList();
            foreach (var claim in claims)
            {
                if (claim.Type?.Length > Constants.Models.Claim.JwtTypeLength)
                {
                    throw new OAuthRequestException($"Claim '{claim.Type.Substring(0, Constants.Models.Claim.JwtTypeLength)}' is too long.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
                }
                if (claim.Value?.Length > Constants.Models.Claim.ValueLength)
                {
                    throw new OAuthRequestException($"Claim value '{claim.Value.Substring(0, Constants.Models.Claim.ValueLength)}' is too long.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
                }
            }
            return claims;
        }

        private async Task<List<Claim>> HandleAuthorizationCodeResponseAsync(OidcUpParty party, OidcUpSequenceData sequenceData, string code)
        {
            var tokenResponse = await TokenRequestAsync(party.Client, code, sequenceData);
            return await ValidateTokens(party, sequenceData, tokenResponse.IdToken, tokenResponse.AccessToken);
        }

        private async Task<TokenResponse> TokenRequestAsync(OidcUpClient client, string code, OidcUpSequenceData sequenceData)
        {
            var tokenRequest = new TokenRequest
            {
                GrantType = IdentityConstants.GrantTypes.AuthorizationCode,
                Code = code,
                ClientId = client.ClientId,
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

            if (client.RequirePkce)
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

        private async Task<List<Claim>> ValidateTokens(OidcUpParty party, OidcUpSequenceData sequenceData, string idToken, string accessToken)
        {
            var claims = await ValidateIdToken(party, sequenceData, idToken);
            claims.AddClaim(Constants.JwtClaimTypes.IdToken, $"{party.Name}|{idToken}");
            if (!accessToken.IsNullOrWhiteSpace())
            {
                await ValidateAccessToken(party, sequenceData, accessToken);
                claims.AddClaim(Constants.JwtClaimTypes.AccessToken, $"{party.Name}|{accessToken}");
            }

            return claims;
        }


        private async Task<List<Claim>> ValidateIdToken(OidcUpParty party, OidcUpSequenceData sequenceData, string idToken)
        {
            try
            {
                (var claimsPrincipal, _) = await Task.FromResult(JwtHandler.ValidateToken(idToken, party.Issuer, party.Keys, sequenceData.ClientId));

                var nonce = claimsPrincipal.Claims.Where(c => c.Type == JwtClaimTypes.Nonce).Select(c => c.Value).SingleOrDefault();
                if (!sequenceData.Nonce.Equals(nonce, StringComparison.Ordinal))
                {
                    throw new OAuthRequestException($"{party.Name}|Id token nonce do not match.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
                }

                var claims = new List<Claim>(claimsPrincipal.Claims.Where(c => c.Type != JwtClaimTypes.Subject));
                var subject = claimsPrincipal.Claims.FindFirstValue(c => c.Type == JwtClaimTypes.Subject);
                if (!subject.IsNullOrEmpty())
                {
                    claims.Add(new Claim(JwtClaimTypes.Subject, $"{party.Name}|{subject}"));
                }
                return claims;
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

        private async Task ValidateAccessToken(OidcUpParty party, OidcUpSequenceData sequenceData, string accessToken)
        {
            try
            {
                (_, _) = await Task.FromResult(JwtHandler.ValidateToken(accessToken, party.Issuer, party.Keys, sequenceData.ClientId, validateAudience: false));
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
                        return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequenceData.DownPartyId, ErrorToSamlStatus(error), await claimsLogic.FromJwtToSamlClaimsAsync(claims));

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
