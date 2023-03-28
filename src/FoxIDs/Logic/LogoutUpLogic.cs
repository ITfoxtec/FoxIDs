using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;

namespace FoxIDs.Logic
{
    public class LogoutUpLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly SequenceLogic sequenceLogic;

        public LogoutUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, SequenceLogic sequenceLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.sequenceLogic = sequenceLogic;
        }

        public async Task<IActionResult> LogoutRedirect(UpPartyLink partyLink, LogoutRequest logoutRequest)
        {
            logger.ScopeTrace(() => "Down, Logout redirect.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            await logoutRequest.ValidateObjectAsync();

            await sequenceLogic.SetUiUpPartyIdAsync(partyId);
            await sequenceLogic.SaveSequenceDataAsync(new LoginUpSequenceData
            {
                DownPartyLink = logoutRequest.DownPartyLink,
                UpPartyId = partyId,
                SessionId = logoutRequest.SessionId,
                RequireLogoutConsent = logoutRequest.RequireLogoutConsent,
                PostLogoutRedirect = logoutRequest.PostLogoutRedirect
            });

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.LoginController, Constants.Endpoints.Logout, includeSequence: true).ToRedirectResult();
        }

        public async Task<IActionResult> LogoutResponseAsync(LoginUpSequenceData sequenceData)
        {
            logger.ScopeTrace(() => "Down, Logout response.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, sequenceData.UpPartyId);

            logger.ScopeTrace(() => $"Response, Down type {sequenceData.DownPartyLink.Type}.");
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
    }
}
