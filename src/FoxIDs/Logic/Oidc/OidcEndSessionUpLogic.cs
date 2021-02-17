using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UrlCombineLib;

namespace FoxIDs.Logic
{
    public class OidcEndSessionUpLogic<TParty, TClient> : LogicBase where TParty : OidcUpParty<TClient> where TClient : OidcUpClient
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;

        public OidcEndSessionUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
        }

        public async Task<IActionResult> EndSessionRequestAsync(UpPartyLink partyLink, LogoutRequest logoutRequest)
        {
            logger.ScopeTrace("Up, OIDC End session request.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty("upPartyId", partyId);

            await logoutRequest.ValidateObjectAsync();

            var party = await tenantRepository.GetAsync<OidcUpParty>(partyId);
            logger.SetScopeProperty("upPartyClientId", party.Client.ClientId);
            ValidatePartyLogoutSupport(party);

            await sequenceLogic.SaveSequenceDataAsync(new OidcUpSequenceData
            {
                DownPartyId = logoutRequest.DownParty.Id,
                DownPartyType = logoutRequest.DownParty.Type,
                SessionId = logoutRequest.SessionId,
            });

            var postLogoutRedirectUrl = UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName, RouteBinding.PartyNameAndBinding, Constants.Routes.OAuthController, Constants.Endpoints.EndSessionnResponse);
            var endSessionRequest = new EndSessionRequest
            {
                IdTokenHint = logoutRequest.IdTokenHint,
                PostLogoutRedirectUri = postLogoutRedirectUrl,
                State = SequenceString
            };
            logger.ScopeTrace($"End session request '{endSessionRequest.ToJsonIndented()}'.");

            var requestDictionary = endSessionRequest.ToDictionary();
            var endSessionRequestUrl = QueryHelpers.AddQueryString(party.Client.EndSessionUrl, requestDictionary);
            logger.ScopeTrace($"End session request URL '{endSessionRequestUrl}'.");
            logger.ScopeTrace("Up, Sending OIDC End session request.", triggerEvent: true);
            return new RedirectResult(endSessionRequestUrl);
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
            var endSessionResponse = queryDictionary.ToObject<EndSessionResponse>();
            logger.ScopeTrace($"End session response '{endSessionResponse.ToJsonIndented()}'.");
            if (endSessionResponse.State.IsNullOrEmpty()) throw new ArgumentNullException(nameof(endSessionResponse.State), endSessionResponse.GetTypeName());

            await sequenceLogic.ValidateSequenceAsync(endSessionResponse.State);
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<OidcUpSequenceData>(remove: true);

            logger.ScopeTrace("Up, Successful OIDC End session response.", triggerEvent: true);

            return await LogoutResponseDownAsync(sequenceData);
        }

        private async Task<IActionResult> LogoutResponseDownAsync(OidcUpSequenceData sequenceData)
        {
            try
            {
                logger.ScopeTrace($"Response, Down type {sequenceData.DownPartyType}.");
                switch (sequenceData.DownPartyType)
                {
                    case PartyTypes.OAuth2:
                        throw new NotImplementedException();
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcEndSessionDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().EndSessionResponseAsync(sequenceData.DownPartyId);
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutDownLogic>().LogoutResponseAsync(sequenceData.DownPartyId, sessionIndex: sequenceData.SessionId);

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
