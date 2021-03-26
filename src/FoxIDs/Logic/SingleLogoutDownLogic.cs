using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Sequences;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<IActionResult> StartSingleLogoutAsync(string sessionId, UpPartyLink upPartyLink, DownPartyLink initiatingDownParty, List<DownPartyLink> downPartyLinks)
        {
            logger.ScopeTrace("Start single logout.");

            downPartyLinks = new List<DownPartyLink>(downPartyLinks.Where(p => p.Id != initiatingDownParty.Id));
            var sequenceData = new SingleLogoutSequenceData
            {
                SessionId = sessionId,
                UpPartyName = upPartyLink.Name,
                UpPartyType = upPartyLink.Type,
                DownPartyId = initiatingDownParty.Id,
                DownPartyType = initiatingDownParty.Type,
                DownPartyLinks = downPartyLinks
            };

            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
            return await HandleSingleLogoutAsync(sequenceData);
        }

        public async Task<IActionResult> HandleSingleLogoutAsync(SingleLogoutSequenceData sequenceData = null)
        {
            sequenceData = sequenceData ?? await sequenceLogic.GetSequenceDataAsync<SingleLogoutSequenceData>(remove: false);

            var oidcDownPartyIds = sequenceData.DownPartyLinks.Where(p => p.Type == PartyTypes.Oidc);
            if(oidcDownPartyIds.Count() > 0)
            {

            }


            

            logger.ScopeTrace($"Response, Up type {sequenceData.UpPartyType}.");
            switch (sequenceData.DownPartyType)
            {
                case PartyTypes.Login:
                    return HttpContext.GetUpPartyUrl(sequenceData.UpPartyName, Constants.Routes.LoginController, Constants.Endpoints.SingleLogoutDone, includeSequence: true).ToRedirectResult();
                case PartyTypes.Oidc:
                    return HttpContext.GetUpPartyUrl(sequenceData.UpPartyName, Constants.Routes.SamlController, Constants.Endpoints.SingleLogoutDone, includeSequence: true).ToRedirectResult();
                case PartyTypes.Saml2:
                    return HttpContext.GetUpPartyUrl(sequenceData.UpPartyName, Constants.Routes.SamlController, Constants.Endpoints.SingleLogoutDone, includeSequence: true).ToRedirectResult();

                default:
                    throw new NotSupportedException();
            }
        }

    }
}
