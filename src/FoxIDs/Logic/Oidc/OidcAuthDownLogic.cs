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
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Security.Claims;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;
using FoxIDs.Models.Session;

namespace FoxIDs.Logic
{
    public class OidcAuthDownLogic<TParty, TClient, TScope, TClaim> : LogicSequenceBase where TParty : OidcDownParty where TClient : OidcDownClient<TScope, TClaim> where TScope : OidcDownScope<TClaim> where TClaim : OidcDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly OidcJwtDownLogic<TClient, TScope, TClaim> oidcJwtDownLogic;
        private readonly OAuthAuthCodeGrantDownLogic<TClient, TScope, TClaim> oauthAuthCodeGrantDownLogic;
        private readonly OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic;

        public OidcAuthDownLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, SecurityHeaderLogic securityHeaderLogic, ClaimTransformLogic claimTransformLogic, OidcJwtDownLogic<TClient, TScope, TClaim> oidcJwtDownLogic, OAuthAuthCodeGrantDownLogic<TClient, TScope, TClaim> oauthAuthCodeGrantDownLogic, OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.oidcJwtDownLogic = oidcJwtDownLogic;
            this.oauthAuthCodeGrantDownLogic = oauthAuthCodeGrantDownLogic;
            this.oauthResourceScopeDownLogic = oauthResourceScopeDownLogic;
        }

        public async Task<IActionResult> AuthenticationRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "AppReg, OIDC Authentication request.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantRepository.GetAsync<TParty>(partyId);
            if(party.Client == null)
            {
                throw new NotSupportedException("Application Client not configured.");
            }
            logger.SetScopeProperty(Constants.Logs.DownPartyClientId, party.Client.ClientId);
            await sequenceLogic.SetDownPartyAsync(partyId, PartyTypes.Oidc);

            var queryDictionary = HttpContext.Request.Query.ToDictionary();
            var authenticationRequest = queryDictionary.ToObject<AuthenticationRequest>();

            logger.ScopeTrace(() => $"Authentication request '{authenticationRequest.ToJsonIndented()}'.", traceType: TraceTypes.Message);

            var codeChallengeSecret = party.Client.RequirePkce ? queryDictionary.ToObject<CodeChallengeSecret>() : null;
            if (codeChallengeSecret != null)
            {
                logger.ScopeTrace(() => $"CodeChallengeSecret '{codeChallengeSecret.ToJsonIndented()}'.", traceType: TraceTypes.Message);
            }

            try
            {
                ValidateAuthenticationRequest(party.Client, authenticationRequest, codeChallengeSecret);
                logger.ScopeTrace(() => "AppReg, OIDC Authentication request accepted.", triggerEvent: true);

                if(!authenticationRequest.UiLocales.IsNullOrWhiteSpace())
                {
                    await sequenceLogic.SetCultureAsync(authenticationRequest.UiLocales.ToSpaceList());
                }

                await sequenceLogic.SaveSequenceDataAsync(new OidcDownSequenceData
                {
                    ResponseType = authenticationRequest.ResponseType,
                    RestrictFormAction = party.RestrictFormAction,
                    RedirectUri = authenticationRequest.RedirectUri,
                    Scope = authenticationRequest.Scope,
                    State = authenticationRequest.State,
                    ResponseMode = authenticationRequest.ResponseMode,
                    Nonce = authenticationRequest.Nonce,
                    CodeChallenge = codeChallengeSecret?.CodeChallenge,
                    CodeChallengeMethod = codeChallengeSecret?.CodeChallengeMethod,
                });

                var toUpParties = RouteBinding.ToUpParties;
                if (toUpParties.Count() == 1)
                {
                    var toUpParty = toUpParties.First();
                    logger.ScopeTrace(() => $"Request, Authentication type '{toUpParty.Type}'.");
                    switch (toUpParty.Type)
                    {
                        case PartyTypes.Login:
                            return await serviceProvider.GetService<LoginUpLogic>().LoginRedirectAsync(toUpParty, await GetLoginRequestAsync(party, authenticationRequest));
                        case PartyTypes.OAuth2:
                            throw new NotImplementedException();
                        case PartyTypes.Oidc:
                            return await serviceProvider.GetService<OidcAuthUpLogic<OidcUpParty, OidcUpClient>>().AuthenticationRequestRedirectAsync(toUpParty, await GetLoginRequestAsync(party, authenticationRequest));
                        case PartyTypes.Saml2:
                            return await serviceProvider.GetService<SamlAuthnUpLogic>().AuthnRequestRedirectAsync(toUpParty, await GetLoginRequestAsync(party, authenticationRequest));
                        case PartyTypes.TrackLink:
                            return await serviceProvider.GetService<TrackLinkAuthUpLogic>().AuthRequestAsync(toUpParty, await GetLoginRequestAsync(party, authenticationRequest));
                        default:
                            throw new NotSupportedException($"Connection type '{toUpParty.Type}' not supported.");
                    }
                }
                else
                {
                    return await serviceProvider.GetService<LoginUpLogic>().LoginRedirectAsync(await GetLoginRequestAsync(party, authenticationRequest));
                }
            }
            catch (OAuthRequestException ex)
            {
                if (authenticationRequest.RedirectUri.IsNullOrWhiteSpace())
                {
                    throw new EndpointException("Redirect URI in authentication request is empty.", ex);
                }
                logger.Error(ex);
                return await AuthenticationResponseErrorAsync(party, authenticationRequest, ex);
            }
        }

        private async Task<LoginRequest> GetLoginRequestAsync(TParty party, AuthenticationRequest authenticationRequest)
        {
            var loginRequest = new LoginRequest { DownPartyLink = new DownPartySessionLink { SupportSingleLogout = !string.IsNullOrWhiteSpace(party.Client?.FrontChannelLogoutUri), Id = party.Id, Type = party.Type } };

            loginRequest.LoginAction = GetLoginAction(authenticationRequest);

            if (authenticationRequest.MaxAge.HasValue)
            {
                loginRequest.MaxAge = authenticationRequest.MaxAge.Value;
            }

            if (!authenticationRequest.IdTokenHint.IsNullOrEmpty())
            {
                var claimsPrincipal = await oidcJwtDownLogic.ValidatePartyClientTokenAsync(party.Client as TClient, authenticationRequest.IdTokenHint, validateLifetime: false);
                if (claimsPrincipal == null)
                {
                    throw new OAuthRequestException("Invalid id token hint.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
                }
                loginRequest.UserId = claimsPrincipal.FindFirst(JwtClaimTypes.Subject).Value;
            }

            if (!authenticationRequest.LoginHint.IsNullOrEmpty())
            {
                loginRequest.EmailHint = authenticationRequest.LoginHint;
            }

            if (!authenticationRequest.AcrValues.IsNullOrWhiteSpace())
            {
                loginRequest.Acr = authenticationRequest.AcrValues.ToSpaceList();
            }

            return loginRequest;
        }

        private LoginAction GetLoginAction(AuthenticationRequest authenticationRequest)
        {
            if (!authenticationRequest.Prompt.IsNullOrWhiteSpace())
            {
                if (authenticationRequest.Prompt.Contains(IdentityConstants.AuthorizationServerPrompt.None))
                {
                    return LoginAction.ReadSession;
                }
                else if (authenticationRequest.Prompt.Contains(IdentityConstants.AuthorizationServerPrompt.Login))
                {
                    return LoginAction.SessionUserRequireLogin;
                }
                else if (authenticationRequest.Prompt.Contains(IdentityConstants.AuthorizationServerPrompt.SelectAccount))
                {
                    return LoginAction.RequireLogin;
                }
            }

            return LoginAction.ReadSessionOrLogin;
        }

        private void ValidateAuthenticationRequest(OidcDownClient client, AuthenticationRequest authenticationRequest, CodeChallengeSecret codeChallengeSecret)
        {
            try
            {
                var responseTypes = authenticationRequest.ResponseType.ToSpaceList();
                bool isImplicitFlow = !responseTypes.Where(rt => rt.Contains(IdentityConstants.ResponseTypes.Code)).Any();
                authenticationRequest.Validate(isImplicitFlow);

                if (client.RequirePkce)
                {
                    if(responseTypes.Where(rt => !rt.Equals(IdentityConstants.ResponseTypes.Code)).Any())
                    {
                        throw new OAuthRequestException($"Require '{IdentityConstants.ResponseTypes.Code}' flow with PKCE.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
                    }
                }

                if (!client.RedirectUris.Any(u => client.DisableAbsoluteUris ? authenticationRequest.RedirectUri?.StartsWith(u, StringComparison.InvariantCultureIgnoreCase) == true : u.Equals(authenticationRequest.RedirectUri, StringComparison.InvariantCultureIgnoreCase)))
                {
                    throw new OAuthRequestException($"Invalid redirect URI '{authenticationRequest.RedirectUri}' (maybe the request URL do not match the expected client).") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
                }

                if (!client.ClientId.Equals(authenticationRequest.ClientId, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new OAuthRequestException($"Invalid client id '{authenticationRequest.ClientId}' (maybe the request URL do not match the expected client).") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidClient };
                }

                if (!authenticationRequest.Scope.Contains(IdentityConstants.DefaultOidcScopes.OpenId))
                {
                    throw new OAuthRequestException($"Require '{IdentityConstants.DefaultOidcScopes.OpenId}' scope.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidScope };
                }
                var resourceScopes = oauthResourceScopeDownLogic.GetResourceScopes(client as TClient);
                var invalidScope = authenticationRequest.Scope.ToSpaceList().Where(s => !(resourceScopes.Select(rs => rs).Contains(s) || (client.Scopes != null && client.Scopes.Select(ps => ps.Scope).Contains(s))) && IdentityConstants.DefaultOidcScopes.OpenId != s);
                if (invalidScope.Count() > 0)
                {
                    throw new OAuthRequestException($"Invalid scope '{authenticationRequest.Scope}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidScope };
                }

                ValidateResponseType(client, authenticationRequest, responseTypes);

                if (!authenticationRequest.ResponseMode.IsNullOrEmpty())
                {
                    var invalidResponseMode = !(new[] { IdentityConstants.ResponseModes.Fragment, IdentityConstants.ResponseModes.Query, IdentityConstants.ResponseModes.FormPost }.Contains(authenticationRequest.ResponseMode));
                    if (invalidResponseMode)
                    {
                        throw new OAuthRequestException($"Invalid response mode '{authenticationRequest.ResponseMode}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
                    }
                }

                if (client.RequirePkce)
                {
                    codeChallengeSecret.Validate();
                }
            }
            catch (ArgumentException ex)
            {
                throw new OAuthRequestException($"{ex.Message}{(ex is ArgumentNullException ? " is null or empty." : string.Empty)}", ex) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
            }
        }

        private void ValidateResponseType(OidcDownClient client, AuthenticationRequest authenticationRequest, string[] responseTypes)
        {
            foreach(var partyResponseTypes in client.ResponseTypes.Select(rt => rt.ToSpaceList()))
            {
                if(responseTypes.Count() == partyResponseTypes.Count())
                {
                    var tempPartyResponseTypes = new List<string>(partyResponseTypes);
                    foreach (var responseTypeItem in responseTypes)
                    {
                        if(tempPartyResponseTypes.Contains(responseTypeItem))
                        {
                            tempPartyResponseTypes.Remove(responseTypeItem);
                        }
                        else
                        {
                            break;
                        }
                    }
                    if(tempPartyResponseTypes.Count() == 0)
                    {
                        //All Response Types match.
                        return;
                    }
                }
            }

            throw new OAuthRequestException($"Unsupported response type '{authenticationRequest.ResponseType}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.UnsupportedResponseType };
        }

        public async Task<IActionResult> AuthenticationResponseAsync(string partyId, List<Claim> claims)
        {
            logger.ScopeTrace(() => "AppReg, OIDC Authentication response.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantRepository.GetAsync<TParty>(partyId);
            if (party.Client == null)
            {
                throw new NotSupportedException("Application Client not configured.");
            }

            var sequenceData = await sequenceLogic.GetSequenceDataAsync<OidcDownSequenceData>(false);

            logger.ScopeTrace(() => $"AppReg, OIDC received JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            claims = await claimTransformLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
            logger.ScopeTrace(() => $"AppReg, OIDC output JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);

            var nameValueCollection = await CreateAuthenticationAndSessionResponse(party, claims, sequenceData);

            var responseMode = GetResponseMode(sequenceData.ResponseMode, sequenceData.ResponseType);

            if (party.RestrictFormAction)
            {
                securityHeaderLogic.AddFormAction(sequenceData.RedirectUri);
            }
            else
            {
                securityHeaderLogic.AddFormActionAllowAll();
            }
            switch (responseMode)
            {
                case IdentityConstants.ResponseModes.FormPost:
                    return await nameValueCollection.ToHtmlPostContentResultAsync(sequenceData.RedirectUri, RouteBinding.DisplayName);
                case IdentityConstants.ResponseModes.Query:
                    return await nameValueCollection.ToRedirectResultAsync(sequenceData.RedirectUri, RouteBinding.DisplayName);
                case IdentityConstants.ResponseModes.Fragment:
                    return await nameValueCollection.ToFragmentResultAsync(sequenceData.RedirectUri, RouteBinding.DisplayName);

                default:
                    throw new NotSupportedException();
            }
        }

        private async Task<Dictionary<string, string>> CreateAuthenticationAndSessionResponse(TParty party, List<Claim> claims, OidcDownSequenceData sequenceData)
        {
            try
            {
                var authenticationResponse = new AuthenticationResponse
                {
                    State = sequenceData.State,
                    ExpiresIn = party.Client.AccessTokenLifetime,
                };
                var sessionResponse = new SessionResponse
                {
                    SessionState = claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.SessionId).GetSessionStateValue(party.Client.ClientId, sequenceData.RedirectUri)
                };

                logger.ScopeTrace(() => $"Response type '{sequenceData.ResponseType}'.");
                var responseTypes = sequenceData.ResponseType.ToSpaceList();

                if (responseTypes.Where(rt => rt.Contains(IdentityConstants.ResponseTypes.Code)).Any())
                {
                    authenticationResponse.Code = await oauthAuthCodeGrantDownLogic.CreateAuthCodeGrantAsync(party.Client as TClient, claims, sequenceData.RedirectUri, sequenceData.Scope, sequenceData.Nonce, sequenceData.CodeChallenge, sequenceData.CodeChallengeMethod);
                }

                string algorithm = IdentityConstants.Algorithms.Asymmetric.RS256;
                if (responseTypes.Where(rt => rt.Contains(IdentityConstants.ResponseTypes.Token)).Any())
                {
                    authenticationResponse.TokenType = IdentityConstants.TokenTypes.Bearer;
                    authenticationResponse.AccessToken = await oidcJwtDownLogic.CreateAccessTokenAsync(party.Client as TClient, claims, sequenceData.Scope?.ToSpaceList(), algorithm);
                }
                if (responseTypes.Where(rt => rt.Contains(IdentityConstants.ResponseTypes.IdToken)).Any())
                {
                    authenticationResponse.IdToken = await oidcJwtDownLogic.CreateIdTokenAsync(party.Client as TClient, claims, sequenceData.Scope?.ToSpaceList(), sequenceData.Nonce, responseTypes, authenticationResponse.Code, authenticationResponse.AccessToken, algorithm);
                }

                logger.ScopeTrace(() => $"Authentication response '{authenticationResponse.ToJsonIndented()}'.", traceType: TraceTypes.Message);

                var nameValueCollection = authenticationResponse.ToDictionary();
                if (!sessionResponse.SessionState.IsNullOrWhiteSpace())
                {
                    logger.ScopeTrace(() => $"Session response '{sessionResponse.ToJsonIndented()}'.", traceType: TraceTypes.Message);
                    nameValueCollection = nameValueCollection.AddToDictionary(sessionResponse);
                }

                logger.ScopeTrace(() => $"Redirect URI '{sequenceData.RedirectUri}'.");
                logger.ScopeTrace(() => "AppReg, OIDC Authentication response.", triggerEvent: true);
                return nameValueCollection;
            }
            catch (KeyException kex)
            {
                var errorAuthenticationResponse = new AuthenticationResponse
                {
                    State = sequenceData.State,
                    Error = IdentityConstants.ResponseErrors.ServerError,
                    ErrorDescription = kex.Message
                };
                return errorAuthenticationResponse.ToDictionary();
            }
        }

        private string GetResponseMode(string responseMode, string responseType)
        {
            if (!responseMode.IsNullOrEmpty())
            {
                logger.ScopeTrace(() => $"Response mode '{responseMode}'.");
                return responseMode;
            }
            else
            {
                var defaultResponseMode = responseType.ToSpaceList().Contains(IdentityConstants.ResponseTypes.Code) ? IdentityConstants.ResponseModes.Query : IdentityConstants.ResponseModes.Fragment;
                logger.ScopeTrace(() => $"Default response mode '{defaultResponseMode}'.");
                return defaultResponseMode;
            }
        }

        public async Task<IActionResult> AuthenticationResponseErrorAsync(string partyId, string error, string errorDescription = null)
        {
            logger.ScopeTrace(() => "AppReg, OIDC Authentication error response.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);

            var sequenceData = await sequenceLogic.GetSequenceDataAsync<OidcDownSequenceData>(false);

            return await AuthenticationResponseErrorAsync(sequenceData.RestrictFormAction, sequenceData.RedirectUri, sequenceData.State, error, errorDescription);
        }

        private Task<IActionResult> AuthenticationResponseErrorAsync(TParty party, AuthenticationRequest authenticationRequest, OAuthRequestException ex)
        {
            logger.ScopeTrace(() => "OIDC Authentication error response.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, party.Id);

            return AuthenticationResponseErrorAsync(party.RestrictFormAction, authenticationRequest.RedirectUri, authenticationRequest.State, ex.Error, ex.ErrorDescription);
        }

        private async Task<IActionResult> AuthenticationResponseErrorAsync(bool restrictFormAction, string redirectUri, string state, string error, string errorDescription)
        {
            var authenticationResponse = new AuthenticationResponse
            {
                State = state,
                Error = error,
                ErrorDescription = errorDescription,
            };

            logger.ScopeTrace(() => $"Authentication error response '{authenticationResponse.ToJsonIndented()}'.", traceType: TraceTypes.Message);
            var nameValueCollection = authenticationResponse.ToDictionary();

            logger.ScopeTrace(() => $"Redirect URI '{redirectUri}'.");

            if (restrictFormAction)
            {
                securityHeaderLogic.AddFormAction(redirectUri);
            }
            else
            {
                securityHeaderLogic.AddFormActionAllowAll();
            }
            return await nameValueCollection.ToRedirectResultAsync(redirectUri, RouteBinding.DisplayName);
        }
    }
}
