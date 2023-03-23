using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Sequences;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class TrackLinkFrontChannelLogoutDownLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantRepository;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly SequenceLogic sequenceLogic;
        private readonly SingleLogoutDownLogic singleLogoutDownLogic;

        public TrackLinkFrontChannelLogoutDownLogic(TelemetryScopedLogger logger, ITenantRepository tenantRepository, SecurityHeaderLogic securityHeaderLogic, SequenceLogic sequenceLogic, SingleLogoutDownLogic singleLogoutDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.securityHeaderLogic = securityHeaderLogic;
            this.sequenceLogic = sequenceLogic;
            this.singleLogoutDownLogic = singleLogoutDownLogic;
        }

        public async Task<IActionResult> LogoutRequestAsync(IEnumerable<string> partyIds, SingleLogoutSequenceData sequenceData, bool hostedInIframe, bool doSamlLogoutInIframe)
        {
            logger.ScopeTrace(() => "Down, Track link front channel logout request.");

            TrackLinkDownParty firstParty = null;
            var partyNames = new List<string>();
            var partyLogoutUrls = new List<string>();
            foreach (var partyId in partyIds)
            {
                var party = await tenantRepository.GetAsync<TrackLinkDownParty>(partyId);
                firstParty = party;

                partyNames.Add(party.Name);
                var frontChannelLogoutUri = HttpContext.GetTrackUpPartyUrl(party.ToUpTrackName, party.ToUpPartyName, Constants.Routes.TrackLinkController, Constants.Endpoints.FrontChannelLogout, includeKeySequence: true);
                partyLogoutUrls.Add(frontChannelLogoutUri);
            }

            if (partyLogoutUrls.Count() <= 0 || partyNames.Count() <= 0 || firstParty == null)
            {
                throw new InvalidOperationException("Unable to complete front channel logout. Please close the browser to logout.");
            }

            await sequenceLogic.SaveSequenceDataAsync(new TrackLinkDownSequenceData { KeyNames = partyNames, Claims = sequenceData.Claims }, setKeyValidUntil: true);

            if (doSamlLogoutInIframe)
            {
                securityHeaderLogic.AddFrameSrcAllowAll();
                // Start SAML logout
                partyLogoutUrls.Add(GetFrontChannelLogoutDoneUrl(sequenceData, firstParty));
            }
            else
            {
                securityHeaderLogic.AddFrameSrcUrls(partyLogoutUrls);
            }
            string redirectUrl = hostedInIframe ? null : GetFrontChannelLogoutDoneUrl(sequenceData, firstParty);
            return partyLogoutUrls.ToHtmIframePage(redirectUrl, "FoxIDs").ToContentResult();
        }

        private string GetFrontChannelLogoutDoneUrl(SingleLogoutSequenceData sequenceData, TrackLinkDownParty firstParty)
        {
            return HttpContext.GetDownPartyUrl(firstParty.Name, sequenceData.UpPartyName, Constants.Routes.TrackLinkController, Constants.Endpoints.FrontChannelLogoutDone, includeSequence: true, firstParty.PartyBindingPattern);
        }

        public Task<IActionResult> LogoutDoneAsync()
        {
            logger.ScopeTrace(() => "Down, Track link front channel logout done.");
            return singleLogoutDownLogic.HandleSingleLogoutAsync();
        }
    }
}
