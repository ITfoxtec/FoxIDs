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
    public class LogoutUpLogic : LogicBase
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
            logger.ScopeTrace("Down, Logout redirect.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty("upPartyId", partyId);

            await logoutRequest.ValidateObjectAsync();

            await sequenceLogic.SetUiUpPartyIdAsync(partyId);
            await sequenceLogic.SaveSequenceDataAsync(new LoginUpSequenceData
            {
                DownPartyId = logoutRequest.DownParty.Id,
                DownPartyType = logoutRequest.DownParty.Type,
                UpPartyId = partyId,
                SessionId = logoutRequest.SessionId,
                RequireLogoutConsent = logoutRequest.RequireLogoutConsent,
                PostLogoutRedirect = logoutRequest.PostLogoutRedirect
            });

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.LoginController, Constants.Endpoints.Logout, includeSequence: true).ToRedirectResult();
        }

        public async Task<IActionResult> LogoutResponseAsync(LoginUpSequenceData sequenceData)
        {
            logger.ScopeTrace("Down, Logout response.");
            logger.SetScopeProperty("upPartyId", sequenceData.UpPartyId);

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
    }
}
