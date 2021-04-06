using FoxIDs.Infrastructure;
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
using UrlCombineLib;

namespace FoxIDs.Logic
{
    public class OidcRpInitiatedLogoutUpLogic<TParty, TClient> : LogicBase where TParty : OidcUpParty<TClient> where TClient : OidcUpClient
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly SingleLogoutDownLogic singleLogoutDownLogic;
        private readonly OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;

        public OidcRpInitiatedLogoutUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, SessionUpPartyLogic sessionUpPartyLogic, SingleLogoutDownLogic singleLogoutDownLogic, OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic, SecurityHeaderLogic securityHeaderLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.singleLogoutDownLogic = singleLogoutDownLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
            this.securityHeaderLogic = securityHeaderLogic;
        }
        public async Task<IActionResult> EndSessionRequestRedirectAsync(UpPartyLink partyLink, LogoutRequest logoutRequest)
        {
            logger.ScopeTrace("Up, OIDC End session request redirect.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty("upPartyId", partyId);

            await logoutRequest.ValidateObjectAsync();

            await sequenceLogic.SaveSequenceDataAsync(new OidcUpSequenceData
            {
                DownPartyLink = logoutRequest.DownPartyLink,
                UpPartyId = partyId,
                SessionId = logoutRequest.SessionId,
                RequireLogoutConsent = logoutRequest.RequireLogoutConsent,
                PostLogoutRedirect = logoutRequest.PostLogoutRedirect,
            });

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.OAuthUpJumpController, Constants.Endpoints.UpJump.EndSessionRequest, includeSequence: true).ToRedirectResult();
        }

        public async Task<IActionResult> EndSessionRequestAsync(string partyId)
        {
            logger.ScopeTrace("Up, OIDC End session request.");
            var oidcUpSequenceData = await sequenceLogic.GetSequenceDataAsync<OidcUpSequenceData>(remove: false);
            if (!oidcUpSequenceData.UpPartyId.Equals(partyId, StringComparison.Ordinal))
            {
                throw new Exception("Invalid up-party id.");
            }
            logger.SetScopeProperty("upPartyId", oidcUpSequenceData.UpPartyId);

            var party = await tenantRepository.GetAsync<OidcUpParty>(oidcUpSequenceData.UpPartyId);
            logger.SetScopeProperty("upPartyClientId", party.Client.ClientId);
            ValidatePartyLogoutSupport(party);

            var postLogoutRedirectUrl = HttpContext.GetUpPartyUrl(party.Name, Constants.Routes.OAuthController, Constants.Endpoints.EndSessionResponse, partyBindingPattern: party.PartyBindingPattern);
            var rpInitiatedLogoutRequest = new RpInitiatedLogoutRequest
            {
                PostLogoutRedirectUri = postLogoutRedirectUrl,
                State = SequenceString
            };
            var session = await sessionUpPartyLogic.GetSessionAsync(party);
            if(session != null)
            {
                rpInitiatedLogoutRequest.IdTokenHint = session.IdToken;
                try
                {
                    if (!oidcUpSequenceData.SessionId.Equals(session.SessionId, StringComparison.Ordinal))
                    {
                        throw new Exception("Requested session ID do not match up-party session ID.");
                    }
                }
                catch (Exception ex)
                {
                    logger.Warning(ex);
                }
            }
            logger.ScopeTrace($"Up, End session request '{rpInitiatedLogoutRequest.ToJsonIndented()}'.");

            await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsAsync(oidcUpSequenceData.SessionId);

            securityHeaderLogic.AddFormActionAllowAll();

            var nameValueCollection = rpInitiatedLogoutRequest.ToDictionary();
            logger.ScopeTrace($"Up, End session request URL '{party.Client.EndSessionUrl}'.");
            logger.ScopeTrace("Up, Sending OIDC End session request.", triggerEvent: true);
            return await nameValueCollection.ToRedirectResultAsync(party.Client.EndSessionUrl);
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
            logger.ScopeTrace($"Up, OIDC End session response.");
            logger.SetScopeProperty("upPartyId", partyId);

            var party = await tenantRepository.GetAsync<OidcUpParty>(partyId);
            logger.SetScopeProperty("upPartyClientId", party.Client.ClientId);

            var queryDictionary = HttpContext.Request.Query.ToDictionary();
            var rpInitiatedLogoutResponse = queryDictionary.ToObject<RpInitiatedLogoutResponse>();
            logger.ScopeTrace($"Up, End session response '{rpInitiatedLogoutResponse.ToJsonIndented()}'.");
            rpInitiatedLogoutResponse.Validate();
            if (rpInitiatedLogoutResponse.State.IsNullOrEmpty()) throw new ArgumentNullException(nameof(rpInitiatedLogoutResponse.State), rpInitiatedLogoutResponse.GetTypeName());

            await sequenceLogic.ValidateSequenceAsync(rpInitiatedLogoutResponse.State);
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<OidcUpSequenceData>(remove: party.DisableSingleLogout);

            var session = await sessionUpPartyLogic.DeleteSessionAsync();
            logger.ScopeTrace("Up, Successful OIDC End session response.", triggerEvent: true);

            if (party.DisableSingleLogout)
            {
                return await LogoutResponseDownAsync(sequenceData);
            }
            else
            {
                (var doSingleLogout, var singleLogoutSequenceData) = await singleLogoutDownLogic.InitializeSingleLogoutAsync(new UpPartyLink { Name = party.Name, Type = party.Type }, sequenceData.DownPartyLink, session);
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
                throw new Exception("Invalid up-party id.");
            }
            return await LogoutResponseDownAsync(sequenceData);
        }

        private async Task<IActionResult> LogoutResponseDownAsync(OidcUpSequenceData sequenceData)
        {
            try
            {
                logger.ScopeTrace($"Response, Down type {sequenceData.DownPartyLink.Type}.");
                switch (sequenceData.DownPartyLink.Type)
                {
                    case PartyTypes.OAuth2:
                        throw new NotImplementedException();
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcRpInitiatedLogoutDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().EndSessionResponseAsync(sequenceData.DownPartyLink.Id);
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutDownLogic>().LogoutResponseAsync(sequenceData.DownPartyLink.Id, sessionIndex: sequenceData.SessionId);

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
