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

namespace FoxIDs.Logic
{
    public class OidcAuthDownLogic<TParty, TClient, TScope, TClaim> : LogicBase where TParty : OidcDownParty where TClient : OidcDownClient<TScope, TClaim> where TScope : OidcDownScope<TClaim> where TClaim : OidcDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly FormActionLogic formActionLogic;
        private readonly ClaimTransformationsLogic claimTransformationsLogic;
        private readonly JwtDownLogic<TClient, TScope, TClaim> jwtDownLogic;
        private readonly OAuthAuthCodeGrantDownLogic<TClient, TScope, TClaim> oauthAuthCodeGrantDownLogic;
        private readonly OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic;

        public OidcAuthDownLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, FormActionLogic formActionLogic, ClaimTransformationsLogic claimTransformationsLogic, JwtDownLogic<TClient, TScope, TClaim> jwtDownLogic, OAuthAuthCodeGrantDownLogic<TClient, TScope, TClaim> oauthAuthCodeGrantDownLogic, OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.formActionLogic = formActionLogic;
            this.claimTransformationsLogic = claimTransformationsLogic;
            this.jwtDownLogic = jwtDownLogic;
            this.oauthAuthCodeGrantDownLogic = oauthAuthCodeGrantDownLogic;
            this.oauthResourceScopeDownLogic = oauthResourceScopeDownLogic;
        }

        public async Task<IActionResult> AuthenticationRequestAsync(string partyId)
        {
            logger.ScopeTrace("Down, OIDC Authentication request.");
            logger.SetScopeProperty("downPartyId", partyId);
            var party = await tenantRepository.GetAsync<TParty>(partyId);
            if(party.Client == null)
            {
                throw new NotSupportedException($"Party Client not configured.");
            }
            logger.SetScopeProperty("downPartyClientId", party.Client.ClientId);

            var queryDictionary = HttpContext.Request.Query.ToDictionary();
            var authenticationRequest = queryDictionary.ToObject<AuthenticationRequest>();

            logger.ScopeTrace($"Authentication request '{authenticationRequest.ToJsonIndented()}'.");

            var codeChallengeSecret = party.Client.RequirePkce ? queryDictionary.ToObject<CodeChallengeSecret>() : null;
            if (codeChallengeSecret != null)
            {
                logger.ScopeTrace($"CodeChallengeSecret '{codeChallengeSecret.ToJsonIndented()}'.");
            }

            try
            {
                ValidateAuthenticationRequest(party.Client, authenticationRequest, codeChallengeSecret);
                logger.ScopeTrace("Down, OIDC Authentication request accepted.", triggerEvent: true);

                if(!authenticationRequest.UiLocales.IsNullOrWhiteSpace())
                {
                    await sequenceLogic.SetCultureAsync(authenticationRequest.UiLocales.ToSpaceList());
                }

                await sequenceLogic.SaveSequenceDataAsync(new OidcDownSequenceData
                {
                    ResponseType = authenticationRequest.ResponseType,
                    RedirectUri = authenticationRequest.RedirectUri,
                    Scope = authenticationRequest.Scope,
                    State = authenticationRequest.State,
                    ResponseMode = authenticationRequest.ResponseMode,
                    Nonce = authenticationRequest.Nonce,
                    CodeChallenge = codeChallengeSecret?.CodeChallenge,
                    CodeChallengeMethod = codeChallengeSecret?.CodeChallengeMethod,
                });
                await formActionLogic.CreateFormActionByUrlAsync(authenticationRequest.RedirectUri);

                var type = RouteBinding.ToUpParties.First().Type;
                logger.ScopeTrace($"Request, Up type '{type}'.");
                switch (type)
                {
                    case PartyTypes.Login:
                        return await serviceProvider.GetService<LoginUpLogic>().LoginRedirectAsync(RouteBinding.ToUpParties.First(), await GetLoginRequestAsync(party, authenticationRequest));
                    case PartyTypes.OAuth2:
                        throw new NotImplementedException();
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcAuthUpLogic<OidcUpParty, OidcUpClient>>().AuthenticationRequestRedirectAsync(RouteBinding.ToUpParties.First(), await GetLoginRequestAsync(party, authenticationRequest));
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlAuthnUpLogic>().AuthnRequestAsync(RouteBinding.ToUpParties.First(), await GetLoginRequestAsync(party, authenticationRequest));

                    default:
                        throw new NotSupportedException($"Party type '{type}' not supported.");
                }

            }
            catch (OAuthRequestException ex)
            {
                logger.Error(ex);
                return await AuthenticationResponseErrorAsync(partyId, authenticationRequest, ex);
            }
        }

        private async Task<LoginRequest> GetLoginRequestAsync(TParty party, AuthenticationRequest authenticationRequest)
        {
            var loginRequest = new LoginRequest { DownParty = party };

            loginRequest.LoginAction = !authenticationRequest.Prompt.IsNullOrWhiteSpace() && authenticationRequest.Prompt.Contains(IdentityConstants.AuthorizationServerPrompt.None) ? LoginAction.ReadSession : LoginAction.ReadSessionOrLogin;

            if(authenticationRequest.MaxAge.HasValue)
            {
                loginRequest.MaxAge = authenticationRequest.MaxAge.Value;
            }

            if (!authenticationRequest.IdTokenHint.IsNullOrEmpty())
            {
                var claimsPrincipal = await jwtDownLogic.ValidatePartyClientTokenAsync(party.Client as TClient, authenticationRequest.IdTokenHint, validateLifetime: false);
                if (claimsPrincipal == null)
                {
                    throw new OAuthRequestException("Invalid id token hint.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
                }
                loginRequest.UserId = claimsPrincipal.FindFirst(JwtClaimTypes.Subject).Value;
            }

            if (!authenticationRequest.LoginHint.IsNullOrEmpty())
            {
                loginRequest.UserId = authenticationRequest.LoginHint;
            }

            return loginRequest;
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

                if (!client.RedirectUris.Any(u => u.Equals(authenticationRequest.RedirectUri, StringComparison.InvariantCultureIgnoreCase)))
                {
                    throw new OAuthRequestException($"Invalid redirect Uri '{authenticationRequest.RedirectUri}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
                }

                if (!client.ClientId.Equals(authenticationRequest.ClientId, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new OAuthRequestException($"Invalid client id '{authenticationRequest.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidClient };
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
                throw new OAuthRequestException(ex.Message, ex) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
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
            logger.ScopeTrace("Down, OIDC Authentication response.");
            logger.SetScopeProperty("downPartyId", partyId);
            var party = await tenantRepository.GetAsync<TParty>(partyId);
            if (party.Client == null)
            {
                throw new NotSupportedException($"Party Client not configured.");
            }

            var sequenceData = await sequenceLogic.GetSequenceDataAsync<OidcDownSequenceData>(false);

            claims = await claimTransformationsLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);

            var authenticationResponse = new AuthenticationResponse
            {
                State = sequenceData.State,
                ExpiresIn = party.Client.AccessTokenLifetime,
            };
            var sessionResponse = new SessionResponse
            {
                SessionState = claims.FindFirstValue(c => c.Type == JwtClaimTypes.SessionId)
            };

            logger.ScopeTrace($"Response type '{sequenceData.ResponseType}'.");
            var responseTypes = sequenceData.ResponseType.ToSpaceList();

            if (responseTypes.Where(rt => rt.Contains(IdentityConstants.ResponseTypes.Code)).Any())
            {
                authenticationResponse.Code = await oauthAuthCodeGrantDownLogic.CreateAuthCodeGrantAsync(party.Client as TClient, claims, sequenceData.RedirectUri, sequenceData.Scope, sequenceData.Nonce, sequenceData.CodeChallenge, sequenceData.CodeChallengeMethod);
            }

            string algorithm = IdentityConstants.Algorithms.Asymmetric.RS256;                
            if (responseTypes.Where(rt => rt.Contains(IdentityConstants.ResponseTypes.Token)).Any())
            {
                authenticationResponse.TokenType = IdentityConstants.TokenTypes.Bearer;
                authenticationResponse.AccessToken = await jwtDownLogic.CreateAccessTokenAsync(party.Client as TClient, claims, sequenceData.Scope?.ToSpaceList(), algorithm);
            }
            if (responseTypes.Where(rt => rt.Contains(IdentityConstants.ResponseTypes.IdToken)).Any())
            {
                authenticationResponse.IdToken = await jwtDownLogic.CreateIdTokenAsync(party.Client as TClient, claims, sequenceData.Scope?.ToSpaceList(), sequenceData.Nonce, responseTypes, authenticationResponse.Code, authenticationResponse.AccessToken, algorithm);
            }

            logger.ScopeTrace($"Authentication response '{authenticationResponse.ToJsonIndented()}'.");
            var nameValueCollection = authenticationResponse.ToDictionary();
            if(!sessionResponse.SessionState.IsNullOrWhiteSpace())
            {
                logger.ScopeTrace($"Session response '{sessionResponse.ToJsonIndented()}'.");
                nameValueCollection = nameValueCollection.AddToDictionary(sessionResponse);
            }

            logger.ScopeTrace($"Redirect Uri '{sequenceData.RedirectUri}'.");
            logger.ScopeTrace("Down, OIDC Authentication response.", triggerEvent: true);

            var responseMode = GetResponseMode(sequenceData.ResponseMode, sequenceData.ResponseType);
            await sequenceLogic.RemoveSequenceDataAsync<OidcDownSequenceData>();
            await formActionLogic.RemoveFormActionSequenceDataAsync();
            switch (responseMode)
            {
                case IdentityConstants.ResponseModes.FormPost:
                    return await nameValueCollection.ToHtmlPostContentResultAsync(sequenceData.RedirectUri);
                case IdentityConstants.ResponseModes.Query:
                    return await nameValueCollection.ToRedirectResultAsync(sequenceData.RedirectUri);
                case IdentityConstants.ResponseModes.Fragment:
                    return await nameValueCollection.ToFragmentResultAsync(sequenceData.RedirectUri);

                default:
                    throw new NotSupportedException();
            }
        }

        private string GetResponseMode(string responseMode, string responseType)
        {
            if (!responseMode.IsNullOrEmpty())
            {
                logger.ScopeTrace($"Response mode '{responseMode}'.");
                return responseMode;
            }
            else
            {
                var defaultResponseMode = responseType.ToSpaceList().Contains(IdentityConstants.ResponseTypes.Code) ? IdentityConstants.ResponseModes.Query : IdentityConstants.ResponseModes.Fragment;
                logger.ScopeTrace($"Default response mode '{defaultResponseMode}'.");
                return defaultResponseMode;
            }
        }

        public async Task<IActionResult> AuthenticationResponseErrorAsync(string partyId, string error, string errorDescription = null)
        {
            logger.ScopeTrace("Down, OIDC Authentication error response.");
            logger.SetScopeProperty("downPartyId", partyId);

            var sequenceData = await sequenceLogic.GetSequenceDataAsync<OidcDownSequenceData>();
            await formActionLogic.RemoveFormActionSequenceDataAsync();

            return await AuthenticationResponseErrorAsync(sequenceData.RedirectUri, sequenceData.State, error, errorDescription);
        }

        private Task<IActionResult> AuthenticationResponseErrorAsync(string partyId, AuthenticationRequest authenticationRequest, OAuthRequestException ex)
        {
            logger.ScopeTrace("OIDC Authentication error response.");
            logger.SetScopeProperty("downPartyId", partyId);

            return AuthenticationResponseErrorAsync(authenticationRequest.RedirectUri, authenticationRequest.State, ex.Error, ex.ErrorDescription);
        }

        private async Task<IActionResult> AuthenticationResponseErrorAsync(string redirectUri, string state, string error, string errorDescription)
        {
            var authenticationResponse = new AuthenticationResponse
            {
                State = state,
                Error = error,
                ErrorDescription = errorDescription,
            };

            logger.ScopeTrace($"Authentication error response '{authenticationResponse.ToJsonIndented()}'.");
            var nameValueCollection = authenticationResponse.ToDictionary();

            logger.ScopeTrace($"Redirect Uri '{redirectUri}'.");
            return await nameValueCollection.ToRedirectResultAsync(redirectUri);
        }

    }
}
