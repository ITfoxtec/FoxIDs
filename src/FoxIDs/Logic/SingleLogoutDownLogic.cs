using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Sequences;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using FoxIDs.Models.Cookies;
using ITfoxtec.Identity;

namespace FoxIDs.Logic
{
    public class SingleLogoutDownLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly SequenceLogic sequenceLogic;

        public SingleLogoutDownLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, SequenceLogic sequenceLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.sequenceLogic = sequenceLogic;
        }

        public async Task<IActionResult> StartSingleLogoutAsync(UpPartyLink upPartyLink, DownPartyLink initiatingDownParty, SessionBaseCookie session)
        {
            logger.ScopeTrace("Start single logout.");

            var downPartyLinks = session?.DownPartyLinks?.Where(p => p.Id != initiatingDownParty.Id);
            if (!(downPartyLinks?.Count() > 0) || !(session?.Claims?.Count() > 0))
            {
                return ResponseUpParty(upPartyLink.Name, upPartyLink.Type);
            }

            var sequenceData = new SingleLogoutSequenceData
            {
                UpPartyName = upPartyLink.Name,
                UpPartyType = upPartyLink.Type,
                DownPartyId = initiatingDownParty.Id,
                DownPartyType = initiatingDownParty.Type,
                DownPartyLinks = session.DownPartyLinks.Where(p => p.Id != initiatingDownParty.Id)
            };

            if (downPartyLinks.Where(p => p.Type == PartyTypes.Saml2).Any())
            {
                sequenceData.Claims = session.Claims;
            }
            else
            {
                sequenceData.Claims = session.Claims.Where(c => c.Claim == JwtClaimTypes.SessionId);
            }

            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
            return await HandleSingleLogoutAsync(sequenceData);
        }

        public async Task<IActionResult> HandleSingleLogoutAsync(SingleLogoutSequenceData sequenceData = null)
        {
            sequenceData = sequenceData ?? await sequenceLogic.GetSequenceDataAsync<SingleLogoutSequenceData>(remove: false);

            var oidcDownPartyIds = sequenceData.DownPartyLinks.Where(p => p.Type == PartyTypes.Oidc).Select(p => p.Id);
            if (oidcDownPartyIds.Count() > 0)
            {
                sequenceData.DownPartyLinks = sequenceData.DownPartyLinks.Where(p => p.Type != PartyTypes.Oidc);
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return await serviceProvider.GetService<OidcEndSessionDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().SingleLogoutRequestAsync(oidcDownPartyIds, sequenceData);
            }

            var samlDownPartyId = sequenceData.DownPartyLinks.Where(p => p.Type == PartyTypes.Saml2).Select(p => p.Id).FirstOrDefault();
            if (samlDownPartyId != null)
            {
                sequenceData.DownPartyLinks = sequenceData.DownPartyLinks.Where(p => p.Id != samlDownPartyId);
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return await serviceProvider.GetService<SamlLogoutDownLogic>().SingleLogoutRequestAsync(samlDownPartyId, sequenceData);
            }

            await sequenceLogic.RemoveSequenceDataAsync<SingleLogoutSequenceData>();
            logger.ScopeTrace("Successful Single Logout.", triggerEvent: true);
            return ResponseUpParty(sequenceData.UpPartyName, sequenceData.UpPartyType);
        }

        private IActionResult ResponseUpParty(string upPartyName, PartyTypes upPartyType)
        {
            logger.ScopeTrace($"Response, Up type {upPartyType}.");
            switch (upPartyType)
            {
                case PartyTypes.Login:
                    return HttpContext.GetUpPartyUrl(upPartyName, Constants.Routes.LoginController, Constants.Endpoints.SingleLogoutDone, includeSequence: true).ToRedirectResult();
                case PartyTypes.Oidc:
                    return HttpContext.GetUpPartyUrl(upPartyName, Constants.Routes.OAuthController, Constants.Endpoints.SingleLogoutDone, includeSequence: true).ToRedirectResult();
                case PartyTypes.Saml2:
                    return HttpContext.GetUpPartyUrl(upPartyName, Constants.Routes.SamlController, Constants.Endpoints.SingleLogoutDone, includeSequence: true).ToRedirectResult();

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
