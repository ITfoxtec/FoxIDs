﻿using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OidcRpInitiatedLogoutUpLogic<TParty, TClient> : LogicSequenceBase where TParty : OidcUpParty<TClient> where TClient : OidcUpClient
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly SingleLogoutDownLogic singleLogoutDownLogic;
        private readonly OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;

        public OidcRpInitiatedLogoutUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantDataRepository tenantDataRepository, SequenceLogic sequenceLogic, SessionUpPartyLogic sessionUpPartyLogic, SingleLogoutDownLogic singleLogoutDownLogic, OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic, SecurityHeaderLogic securityHeaderLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.sequenceLogic = sequenceLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.singleLogoutDownLogic = singleLogoutDownLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
            this.securityHeaderLogic = securityHeaderLogic;
        }
        public async Task<IActionResult> EndSessionRequestRedirectAsync(UpPartyLink partyLink, LogoutRequest logoutRequest)
        {
            logger.ScopeTrace(() => "AuthMethod, OIDC End session request redirect.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            await logoutRequest.ValidateObjectAsync();

            var party = await tenantDataRepository.GetAsync<OidcUpParty>(partyId);

            await sequenceLogic.SaveSequenceDataAsync(new OidcUpSequenceData
            {
                DownPartyLink = logoutRequest.DownPartyLink,
                UpPartyId = partyId,
                SessionId = logoutRequest.SessionId,
                RequireLogoutConsent = logoutRequest.RequireLogoutConsent,
                PostLogoutRedirect = logoutRequest.PostLogoutRedirect,
            });

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.OAuthUpJumpController, Constants.Endpoints.UpJump.EndSessionRequest, includeSequence: true, partyBindingPattern: party.PartyBindingPattern).ToRedirectResult(RouteBinding.DisplayName);
        }

        public async Task<IActionResult> EndSessionRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "AuthMethod, OIDC End session request.");
            var oidcUpSequenceData = await sequenceLogic.GetSequenceDataAsync<OidcUpSequenceData>(remove: false);
            if (!oidcUpSequenceData.UpPartyId.Equals(partyId, StringComparison.Ordinal))
            {
                throw new Exception("Invalid authentication method id.");
            }
            logger.SetScopeProperty(Constants.Logs.UpPartyId, oidcUpSequenceData.UpPartyId);

            var party = await tenantDataRepository.GetAsync<OidcUpParty>(oidcUpSequenceData.UpPartyId);
            logger.SetScopeProperty(Constants.Logs.UpPartyClientId, party.Client.ClientId);

            var session = await sessionUpPartyLogic.GetSessionAsync(party);
            if (session == null)
            {
                return await SingleLogoutDone(party.Id);
            }

            try
            {
                if (!oidcUpSequenceData.SessionId.Equals(session.SessionId, StringComparison.Ordinal))
                {
                    throw new Exception("Requested session ID do not match authentication method session ID.");
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex);
            }

            oidcUpSequenceData.SessionDownPartyLinks = session.DownPartyLinks;
            oidcUpSequenceData.SessionClaims = session.Claims;
            await sequenceLogic.SaveSequenceDataAsync(oidcUpSequenceData);

            _ = await sessionUpPartyLogic.DeleteSessionAsync(party, session);
            await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsAsync(oidcUpSequenceData.SessionId);

            try
            {
                ValidatePartyLogoutSupport(party);
            }
            catch (Exception ex)
            {
                logger.Warning(ex);
                return await EndSessionResponseInternalAsync(party);
            }

            securityHeaderLogic.AddFormActionAllowAll();

            var postLogoutRedirectUrl = HttpContext.GetUpPartyUrl(party.Name, Constants.Routes.OAuthController, Constants.Endpoints.EndSessionResponse, partyBindingPattern: party.PartyBindingPattern);
            var rpInitiatedLogoutRequest = new RpInitiatedLogoutRequest
            {
                PostLogoutRedirectUri = postLogoutRedirectUrl,
                State = await sequenceLogic.CreateExternalSequenceIdAsync(),
                IdTokenHint = session.IdToken
            };
            logger.ScopeTrace(() => $"AuthMethod, End session request '{rpInitiatedLogoutRequest.ToJson()}'.", traceType: TraceTypes.Message);
            var nameValueCollection = rpInitiatedLogoutRequest.ToDictionary();
            logger.ScopeTrace(() => $"AuthMethod, End session request URL '{party.Client.EndSessionUrl}'.");
            logger.ScopeTrace(() => "AuthMethod, Sending OIDC End session request.", triggerEvent: true);
            return await nameValueCollection.ToRedirectResultAsync(party.Client.EndSessionUrl, RouteBinding.DisplayName);
        }

        private void ValidatePartyLogoutSupport(OidcUpParty party)
        {
            if (party.Client.EndSessionUrl.IsNullOrEmpty())
            {
                throw new EndpointException("End session not configured.") { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> EndSessionResponseAsync(string partyId)
        {
            logger.ScopeTrace(() => $"AuthMethod, OIDC End session response.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            var party = await tenantDataRepository.GetAsync<OidcUpParty>(partyId);
            logger.SetScopeProperty(Constants.Logs.UpPartyClientId, party.Client.ClientId);

            return await EndSessionResponseAsync(party);
        }

        private async Task<IActionResult> EndSessionResponseAsync(OidcUpParty party)
        {
            var queryDictionary = HttpContext.Request.Query.ToDictionary();
            var rpInitiatedLogoutResponse = queryDictionary.ToObject<RpInitiatedLogoutResponse>();
            logger.ScopeTrace(() => $"AuthMethod, End session response '{rpInitiatedLogoutResponse.ToJson()}'.", traceType: TraceTypes.Message);
            rpInitiatedLogoutResponse.Validate();
            if (rpInitiatedLogoutResponse.State.IsNullOrEmpty()) throw new ArgumentNullException(nameof(rpInitiatedLogoutResponse.State), rpInitiatedLogoutResponse.GetTypeName());

            await sequenceLogic.ValidateExternalSequenceIdAsync(rpInitiatedLogoutResponse.State);
            logger.ScopeTrace(() => "AuthMethod, Successful OIDC End session response.", triggerEvent: true);
            return await EndSessionResponseInternalAsync(party);
        }

        private async Task<IActionResult> EndSessionResponseInternalAsync(OidcUpParty party)
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<OidcUpSequenceData>(remove: party.DisableSingleLogout);

            if (party.DisableSingleLogout)
            {
                return await LogoutResponseDownAsync(sequenceData);
            }
            else
            {
                (var doSingleLogout, var singleLogoutSequenceData) = await singleLogoutDownLogic.InitializeSingleLogoutAsync(new UpPartyLink { Name = party.Name, Type = party.Type }, sequenceData.DownPartyLink, sequenceData.SessionDownPartyLinks, sequenceData.SessionClaims);
                if (doSingleLogout)
                {
                    return await singleLogoutDownLogic.StartSingleLogoutAsync(singleLogoutSequenceData);
                }
                else
                {
                    await sequenceLogic.RemoveSequenceDataAsync<OidcUpSequenceData>();
                    return await LogoutResponseDownAsync(sequenceData);
                }
            }
        }

        public async Task<IActionResult> SingleLogoutDone(string partyId)
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<OidcUpSequenceData>(remove: true);
            if (!sequenceData.UpPartyId.Equals(partyId, StringComparison.Ordinal))
            {
                throw new Exception("Invalid authentication method id.");
            }
            return await LogoutResponseDownAsync(sequenceData);
        }

        private async Task<IActionResult> LogoutResponseDownAsync(OidcUpSequenceData sequenceData)
        {
            try
            {
                logger.ScopeTrace(() => $"Response, Application type {sequenceData.DownPartyLink.Type}.");
                switch (sequenceData.DownPartyLink.Type)
                {
                    case PartyTypes.OAuth2:
                        throw new NotImplementedException();
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcRpInitiatedLogoutDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().EndSessionResponseAsync(sequenceData.DownPartyLink.Id);
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutDownLogic>().LogoutResponseAsync(sequenceData.DownPartyLink.Id, sessionIndex: sequenceData.SessionId);
                    case PartyTypes.TrackLink:
                        return await serviceProvider.GetService<TrackLinkRpInitiatedLogoutDownLogic>().LogoutResponseAsync(sequenceData.DownPartyLink.Id);

                    default:
                        throw new NotSupportedException();
                }
            }
            catch (Exception ex)
            {
                throw new StopSequenceException("Falling logout response down", ex);
            }
        }
    }
}
