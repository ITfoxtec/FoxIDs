﻿using System;
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
using FoxIDs.Logic.Tracks;

namespace FoxIDs.Logic
{
    public class OidcRpInitiatedLogoutDownLogic<TParty, TClient, TScope, TClaim> : LogicSequenceBase where TParty : OidcDownParty<TClient, TScope, TClaim> where TClient : OidcDownClient<TScope, TClaim> where TScope : OidcDownScope<TClaim> where TClaim : OidcDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly HrdLogic hrdLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly OidcJwtDownLogic<TClient, TScope, TClaim> oidcJwtDownLogic;

        public OidcRpInitiatedLogoutDownLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantDataRepository tenantDataRepository, SequenceLogic sequenceLogic, HrdLogic hrdLogic, SecurityHeaderLogic securityHeaderLogic, OidcJwtDownLogic<TClient, TScope, TClaim> oidcJwtDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.sequenceLogic = sequenceLogic;
            this.hrdLogic = hrdLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.oidcJwtDownLogic = oidcJwtDownLogic;
        }

        public async Task<IActionResult> EndSessionRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "AppReg, End session request.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantDataRepository.GetAsync<TParty>(partyId);
            if (party.Client == null)
            {
                throw new NotSupportedException("Application Client not configured.");
            }
            await sequenceLogic.SetDownPartyAsync(partyId, PartyTypes.Oidc);

            var formOrQueryDictionary = HttpContext.Request.Method switch
            {
                "POST" => party.Client.ResponseMode == IdentityConstants.ResponseModes.FormPost ? HttpContext.Request.Form.ToDictionary() : throw new NotSupportedException($"POST not supported by response mode '{party.Client.ResponseMode}'."),
                "GET" => party.Client.ResponseMode == IdentityConstants.ResponseModes.Query ? HttpContext.Request.Query.ToDictionary() : throw new NotSupportedException($"GET not supported by response mode '{party.Client.ResponseMode}'."),
                _ => throw new NotSupportedException($"Request method not supported by response mode '{party.Client.ResponseMode}'")
            };
           
            var rpInitiatedLogoutRequest = formOrQueryDictionary.ToObject<RpInitiatedLogoutRequest>();

            try
            {
                if (party.Client.ResponseMode == IdentityConstants.ResponseModes.Query && rpInitiatedLogoutRequest.IdTokenHint?.Count() > Constants.Models.Claim.IdTokenLimitedHintValueLength)
                {
                    throw new Exception("The ID Token hint length is close to the maximum allowed limit and may be truncated. If this happens the ID Token become invalid and is not accepted.");
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex);
            }

            logger.ScopeTrace(() => $"end session request '{rpInitiatedLogoutRequest.ToJson()}'.", traceType: TraceTypes.Message);
            logger.SetScopeProperty(Constants.Logs.DownPartyClientId, party.Client.ClientId);

            ValidateEndSessionRequest(party.Client, rpInitiatedLogoutRequest);
            logger.ScopeTrace(() => "AppReg, OIDC End session request accepted.", triggerEvent: true);

            if (!rpInitiatedLogoutRequest.UiLocales.IsNullOrWhiteSpace())
            {
                await sequenceLogic.SetCultureAsync(rpInitiatedLogoutRequest.UiLocales.ToSpaceList());
            }

            (var validIdToken, var sessionId, var idTokenClaims) = await ValidateIdTokenHintAsync(party, rpInitiatedLogoutRequest.IdTokenHint);
            if (!validIdToken)
            {
                if (party.Client.RequireLogoutIdTokenHint)
                {
                    if (!rpInitiatedLogoutRequest.IdTokenHint.IsNullOrEmpty())
                    {
                        throw new OAuthRequestException($"Invalid ID Token hint.") { RouteBinding = RouteBinding };
                    }

                    throw new OAuthRequestException($"ID Token hint is required.") { RouteBinding = RouteBinding };
                }
            }
            else
            {
                logger.ScopeTrace(() => "Valid ID token hint.");
            }

            var postLogoutRedirectUri = !rpInitiatedLogoutRequest.PostLogoutRedirectUri.IsNullOrWhiteSpace() ? rpInitiatedLogoutRequest.PostLogoutRedirectUri : party.Client.PostLogoutRedirectUri;
            await sequenceLogic.SaveSequenceDataAsync(new OidcDownSequenceData
            {
                RestrictFormAction = party.RestrictFormAction,
                RedirectUri = postLogoutRedirectUri,
                State = rpInitiatedLogoutRequest.State,
            });

            var toUpParty = await GetToUpPartyAsync(idTokenClaims);
            logger.ScopeTrace(() => $"Request, Authentication type '{toUpParty.Type}'.");
            switch (toUpParty.Type)
            {
                case PartyTypes.Login:
                    return await serviceProvider.GetService<LogoutUpLogic>().LogoutRedirect(toUpParty, GetLogoutRequest(party, sessionId, validIdToken, postLogoutRedirectUri));
                case PartyTypes.OAuth2:
                    throw new NotImplementedException();
                case PartyTypes.Oidc:
                    return await serviceProvider.GetService<OidcRpInitiatedLogoutUpLogic<OidcUpParty, OidcUpClient>>().EndSessionRequestRedirectAsync(toUpParty, GetLogoutRequest(party, sessionId, validIdToken, postLogoutRedirectUri));
                case PartyTypes.Saml2:
                    if (!validIdToken)
                    {
                        throw new OAuthRequestException($"ID Token hint is required for SAML 2.0 authentication method.") { RouteBinding = RouteBinding };
                    }
                    return await serviceProvider.GetService<SamlLogoutUpLogic>().LogoutRequestRedirectAsync(toUpParty, GetSamlLogoutRequest(party, sessionId));
                case PartyTypes.TrackLink:
                    return await serviceProvider.GetService<TrackLinkRpInitiatedLogoutUpLogic>().LogoutRequestRedirectAsync(toUpParty, GetLogoutRequest(party, sessionId, validIdToken, postLogoutRedirectUri));
                case PartyTypes.ExternalLogin:
                    return await serviceProvider.GetService<ExternalLogoutUpLogic>().LogoutRedirect(toUpParty, GetLogoutRequest(party, sessionId, validIdToken, postLogoutRedirectUri));

                default:
                    throw new NotSupportedException($"Connection type '{toUpParty.Type}' not supported.");
            }
        }

        private async Task<UpPartyLink> GetToUpPartyAsync(IEnumerable<Claim> idTokenClaims)
        {
            var toUpPartyFromIdToken = GetUpPartyFromIdToken(idTokenClaims);
            if (toUpPartyFromIdToken != null)
            {
                await hrdLogic.DeleteHrdSelectionBySelectedUpPartyAsync(toUpPartyFromIdToken.Name, toUpPartyFromIdToken.ProfileName);
                return toUpPartyFromIdToken;
            }

            (var toUpParties, var isSession) = await serviceProvider.GetService<SessionUpPartyLogic>().GetSessionOrRouteBindingUpParty(RouteBinding.ToUpParties);
            if (isSession && toUpParties?.Count() == 1)
            {
                var sessionUpParty = toUpParties.First();
                await hrdLogic.DeleteHrdSelectionBySelectedUpPartyAsync(sessionUpParty.Name, sessionUpParty.ProfileName);
                return sessionUpParty;
            }

            return await hrdLogic.GetUpPartyAndDeleteHrdSelectionAsync();
        }

        private UpPartyLink GetUpPartyFromIdToken(IEnumerable<Claim> idTokenClaims)
        {
            var upPartyName = idTokenClaims.FindFirstOrDefaultValue(c => c.Type == Constants.JwtClaimTypes.AuthMethod) ?? idTokenClaims.FindFirstOrDefaultValue(c => c.Type == Constants.JwtClaimTypes.UpParty);
            var upPartyProfileName = idTokenClaims.FindFirstOrDefaultValue(c => c.Type == Constants.JwtClaimTypes.AuthProfileMethod);
            var upPartyTypeValue = idTokenClaims.FindFirstOrDefaultValue(c => c.Type == Constants.JwtClaimTypes.AuthMethodType) ?? idTokenClaims.FindFirstOrDefaultValue(c => c.Type == Constants.JwtClaimTypes.UpPartyType);
            if (!upPartyName.IsNullOrWhiteSpace() && !upPartyTypeValue.IsNullOrWhiteSpace() && upPartyTypeValue.TryGetPartyType(out PartyTypes upPartyType))
            {
                return new UpPartyLink { Name = upPartyName, ProfileName = upPartyProfileName, Type = upPartyType };
            }
            return null;
        }

        private LogoutRequest GetLogoutRequest(TParty party, string sessionId, bool validIdToken, string postLogoutRedirectUri)
        {
            var logoutRequest = new LogoutRequest
            {
                DownPartyLink = new DownPartySessionLink { SupportSingleLogout = !string.IsNullOrWhiteSpace(party.Client?.FrontChannelLogoutUri), Id = party.Id, Type = party.Type },
                SessionId = sessionId,
                RequireLogoutConsent = !validIdToken,
                PostLogoutRedirect = !postLogoutRedirectUri.IsNullOrWhiteSpace()
            };

            return logoutRequest;
        }

        private LogoutRequest GetSamlLogoutRequest(TParty party, string sessionId)
        {
            return new LogoutRequest
            {
                DownPartyLink = new DownPartySessionLink { SupportSingleLogout = !string.IsNullOrWhiteSpace(party.Client?.FrontChannelLogoutUri), Id = party.Id, Type = party.Type },
                SessionId = sessionId,
                RequireLogoutConsent = false,
                PostLogoutRedirect = true
            };
        }

        private async Task<(bool, string, IEnumerable<Claim>)> ValidateIdTokenHintAsync(TParty party, string idToken)
        {
            if (!idToken.IsNullOrEmpty())
            {
                var claimsPrincipal = await oidcJwtDownLogic.ValidatePartyClientTokenAsync(party.Client, party.UsePartyIssuer ? RouteBinding.RouteUrl : null, idToken, validateLifetime: false);
                if (claimsPrincipal != null)
                {
                    return (true, claimsPrincipal.FindFirstValue(JwtClaimTypes.SessionId), claimsPrincipal.Claims);
                }
            }
            return (false, null, null);
        }

        private void ValidateEndSessionRequest(TClient client, RpInitiatedLogoutRequest rpInitiatedLogoutRequest)
        {
            rpInitiatedLogoutRequest.Validate();

            if (!rpInitiatedLogoutRequest.PostLogoutRedirectUri.IsNullOrWhiteSpace() && 
                !client.RedirectUris.Any(u => client.DisableAbsoluteUris ? 
                    rpInitiatedLogoutRequest.PostLogoutRedirectUri?.StartsWith(u, StringComparison.InvariantCultureIgnoreCase) == true : 
                    u.Equals(rpInitiatedLogoutRequest.PostLogoutRedirectUri, StringComparison.InvariantCultureIgnoreCase)) &&
                !(client.DisableAbsoluteUris ?
                    client.PostLogoutRedirectUri != null && rpInitiatedLogoutRequest.PostLogoutRedirectUri?.StartsWith(client.PostLogoutRedirectUri, StringComparison.InvariantCultureIgnoreCase) == true : 
                    client.PostLogoutRedirectUri?.Equals(rpInitiatedLogoutRequest.PostLogoutRedirectUri, StringComparison.InvariantCultureIgnoreCase) == true))
            {
                throw new OAuthRequestException($"Invalid post logout redirect URI '{rpInitiatedLogoutRequest.PostLogoutRedirectUri}'.");
            }
        }

        public async Task<IActionResult> EndSessionResponseAsync(string partyId)
        {
            logger.ScopeTrace(() => "AppReg, End session response.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);

            var sequenceData = await sequenceLogic.GetSequenceDataAsync<OidcDownSequenceData>(false);

            var rpInitiatedLogoutResponse = new RpInitiatedLogoutResponse
            {
                State = sequenceData.State,
            };

            logger.ScopeTrace(() => $"End session response '{rpInitiatedLogoutResponse.ToJson()}'.", traceType: TraceTypes.Message);
            var nameValueCollection = rpInitiatedLogoutResponse.ToDictionary();

            logger.ScopeTrace(() => $"Redirect URI '{sequenceData.RedirectUri}'.");
            logger.ScopeTrace(() => "AppReg, OIDC End session response.", triggerEvent: true);

            await sequenceLogic.RemoveSequenceDataAsync<OidcDownSequenceData>();
            if (sequenceData.RestrictFormAction)
            {
                securityHeaderLogic.AddFormAction(sequenceData.RedirectUri);
            }
            else
            {
                securityHeaderLogic.AddFormActionAllowAll();
            }
            return sequenceData.RedirectUri.ToRedirectResult(nameValueCollection);
        }
    }
}
