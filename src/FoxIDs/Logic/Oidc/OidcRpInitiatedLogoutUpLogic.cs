using FoxIDs.Infrastructure;
using FoxIDs.Logic.Tracks;
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
        private readonly HrdLogic hrdLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly StateUpPartyLogic stateUpPartyLogic;
        private readonly SingleLogoutLogic singleLogoutLogic;
        private readonly OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;

        public OidcRpInitiatedLogoutUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantDataRepository tenantDataRepository, SequenceLogic sequenceLogic, HrdLogic hrdLogic, SessionUpPartyLogic sessionUpPartyLogic, StateUpPartyLogic stateUpPartyLogic, SingleLogoutLogic singleLogoutLogic, OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic, SecurityHeaderLogic securityHeaderLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.sequenceLogic = sequenceLogic;
            this.hrdLogic = hrdLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.stateUpPartyLogic = stateUpPartyLogic;
            this.singleLogoutLogic = singleLogoutLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
            this.securityHeaderLogic = securityHeaderLogic;
        }

        public async Task<IActionResult> EndSessionRequestRedirectAsync(UpPartyLink partyLink, LogoutRequest logoutRequest, bool isSingleLogout = false)
        {
            logger.ScopeTrace(() => "AuthMethod, OIDC End session request redirect.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            await logoutRequest.ValidateObjectAsync();

            var party = await tenantDataRepository.GetAsync<OidcUpParty>(partyId);

            await sequenceLogic.SaveSequenceDataAsync(new OidcUpSequenceData
            {
                IsSingleLogout = isSingleLogout,
                DownPartyLink = logoutRequest?.DownPartyLink,
                UpPartyId = partyId,
                SessionId = logoutRequest?.SessionId,
                RequireLogoutConsent = logoutRequest?.RequireLogoutConsent ?? false,
                PostLogoutRedirect = logoutRequest?.PostLogoutRedirect ?? true,
                ClientId = ResolveClientId(party)
            }, partyName: party.Name);

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.OAuthUpJumpController, Constants.Endpoints.UpJump.EndSessionRequest, includeSequence: true, partyBindingPattern: party.PartyBindingPattern).ToRedirectResult();
        }

        public async Task<IActionResult> EndSessionRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "AuthMethod, OIDC End session request.");
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<OidcUpSequenceData>(partyName: partyId.PartyIdToName(), remove: false);
            if (!sequenceData.UpPartyId.Equals(partyId, StringComparison.Ordinal))
            {
                throw new Exception("Invalid authentication method id.");
            }
            logger.SetScopeProperty(Constants.Logs.UpPartyId, sequenceData.UpPartyId);

            var party = await tenantDataRepository.GetAsync<OidcUpParty>(sequenceData.UpPartyId);
            logger.SetScopeProperty(Constants.Logs.UpPartyClientId, party.Client.ClientId);

            var session = await sessionUpPartyLogic.GetSessionAsync(party);
            if (session == null)
            {
                if (sequenceData.IsSingleLogout)
                {
                    return await singleLogoutLogic.HandleSingleLogoutUpAsync();
                }
                else
                {
                    return await SingleLogoutDoneAsync(party.Id);
                }
            }

            try
            {
                if (!sequenceData.SessionId.IsNullOrEmpty() && !sequenceData.SessionId.Equals(session.SessionIdClaim, StringComparison.Ordinal))
                {
                    throw new Exception("Requested session ID do not match authentication method session ID.");
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex);
            }

            await hrdLogic.DeleteHrdSelectionBySelectedUpPartyAsync(party.Name);
            _ = await sessionUpPartyLogic.DeleteSessionAsync(party, session);
            await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsBySessionIdAsync(sequenceData.SessionId);

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
                IdTokenHint = session.IdToken,
                ClientId = sequenceData.ClientId,
                PostLogoutRedirectUri = postLogoutRedirectUrl,
                State = await sequenceLogic.CreateExternalSequenceIdAsync(),
            };
            logger.ScopeTrace(() => $"AuthMethod, End session request '{rpInitiatedLogoutRequest.ToJson()}'.", traceType: TraceTypes.Message);
            var nameValueCollection = rpInitiatedLogoutRequest.ToDictionary();

            if(party.Issuers.Any(i => i.Contains("amazonaws.com", StringComparison.OrdinalIgnoreCase)))
            {
                nameValueCollection.Add("logout_uri", rpInitiatedLogoutRequest.PostLogoutRedirectUri);
                logger.ScopeTrace(() => $"AuthMethod, End session add custom 'logout_uri={rpInitiatedLogoutRequest.PostLogoutRedirectUri}' parameter for Amazon AWS Cognito.");
            }

            await stateUpPartyLogic.CreateOrUpdateStateCookieAsync(party, rpInitiatedLogoutRequest.State);

            logger.ScopeTrace(() => $"AuthMethod, End session request URL '{party.Client.EndSessionUrl}'.");
            logger.ScopeTrace(() => "AuthMethod, Sending OIDC End session request.", triggerEvent: true);
            return party.Client.EndSessionUrl.ToRedirectResult(nameValueCollection);
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

            if (rpInitiatedLogoutResponse.State.IsNullOrEmpty())
            {
                rpInitiatedLogoutResponse.State = await stateUpPartyLogic.GetAndDeleteStateCookieAsync(party);
            }
            else
            {
                await stateUpPartyLogic.DeleteStateCookieAsync(party);
            }

            await sequenceLogic.ValidateExternalSequenceIdAsync(rpInitiatedLogoutResponse.State);
            logger.ScopeTrace(() => "AuthMethod, Successful OIDC End session response.", triggerEvent: true);
            return await EndSessionResponseInternalAsync(party);
        }

        private async Task<IActionResult> EndSessionResponseInternalAsync(OidcUpParty party)
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<OidcUpSequenceData>(partyName: party.Name, remove: party.DisableSingleLogout);

            if (sequenceData.IsSingleLogout)
            {
                await sequenceLogic.RemoveSequenceDataAsync<OidcUpSequenceData>(partyName: party.Name);
                return await singleLogoutLogic.HandleSingleLogoutUpAsync();
            }
            else
            {
                if (party.DisableSingleLogout)
                {
                    await sessionUpPartyLogic.DeleteSessionTrackCookieGroupAsync(party);
                    return await LogoutResponseDownAsync(sequenceData);
                }
                else
                {
                    (var doSingleLogout, var singleLogoutSequenceData) = await singleLogoutLogic.InitializeSingleLogoutAsync(party, sequenceData.DownPartyLink, sequenceData);
                    if (doSingleLogout)
                    {
                        return await singleLogoutLogic.StartSingleLogoutAsync(singleLogoutSequenceData);
                    }
                    else
                    {
                        await sequenceLogic.RemoveSequenceDataAsync<OidcUpSequenceData>(partyName: party.Name);
                        return await LogoutResponseDownAsync(sequenceData);
                    }
                }
            }
        }

        public async Task<IActionResult> SingleLogoutDoneAsync(string partyId)
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<OidcUpSequenceData>(partyName: partyId.PartyIdToName(), remove: true);
            if (!sequenceData.IsSingleLogout && !sequenceData.UpPartyId.Equals(partyId, StringComparison.Ordinal))
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

        protected string ResolveClientId(OidcUpParty party)
        {
            return !party.Client.SpClientId.IsNullOrWhiteSpace() ? party.Client.SpClientId : party.Client.ClientId;
        }
    }
}
