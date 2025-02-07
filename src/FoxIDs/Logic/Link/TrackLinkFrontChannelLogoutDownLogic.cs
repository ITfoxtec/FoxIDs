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
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly SequenceLogic sequenceLogic;
        private readonly SingleLogoutLogic singleLogoutLogic;

        public TrackLinkFrontChannelLogoutDownLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, SecurityHeaderLogic securityHeaderLogic, SequenceLogic sequenceLogic, SingleLogoutLogic singleLogoutLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.securityHeaderLogic = securityHeaderLogic;
            this.sequenceLogic = sequenceLogic;
            this.singleLogoutLogic = singleLogoutLogic;
        }

        public async Task<IActionResult> LogoutRequestAsync(IEnumerable<string> partyIds, SingleLogoutSequenceData sequenceData, bool hostedInIframe, bool doSamlLogoutInIframe)
        {
            logger.ScopeTrace(() => "AppReg, Environment Link front channel logout request.");

            TrackLinkDownParty firstParty = null;
            var partyNames = new List<string>();
            var partyLogoutUrls = new List<string>();
            foreach (var partyId in partyIds)
            {
                var party = await tenantDataRepository.GetAsync<TrackLinkDownParty>(partyId);
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
            return partyLogoutUrls.ToHtmIframePage(redirectUrl, RouteBinding.DisplayName ?? "FoxIDs").ToContentResult();
        }

        private string GetFrontChannelLogoutDoneUrl(SingleLogoutSequenceData sequenceData, TrackLinkDownParty firstParty)
        {
            return HttpContext.GetDownPartyUrl(firstParty.Name, sequenceData.UpPartyId.PartyIdToName(), Constants.Routes.TrackLinkController, Constants.Endpoints.FrontChannelLogoutDone, includeSequence: true, firstParty.PartyBindingPattern);
        }

        public Task<IActionResult> LogoutDoneAsync()
        {
            logger.ScopeTrace(() => "AppReg, Environment Link front channel logout done.");
            return singleLogoutLogic.HandleSingleLogoutDownAsync();
        }
    }
}
