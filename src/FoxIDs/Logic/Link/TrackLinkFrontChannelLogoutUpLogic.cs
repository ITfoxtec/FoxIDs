using FoxIDs.Infrastructure;
using FoxIDs.Logic.Tracks;
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
    public class TrackLinkFrontChannelLogoutUpLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly HrdLogic hrdLogic;
        private readonly SingleLogoutDownLogic singleLogoutDownLogic;

        public TrackLinkFrontChannelLogoutUpLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, SequenceLogic sequenceLogic, SessionUpPartyLogic sessionUpPartyLogic, HrdLogic hrdLogic, SingleLogoutDownLogic singleLogoutDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.sequenceLogic = sequenceLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.hrdLogic = hrdLogic;
            this.singleLogoutDownLogic = singleLogoutDownLogic;
        }

        public async Task<IActionResult> FrontChannelLogoutAsync(string partyId)
        {
            logger.ScopeTrace(() => "AuthMethod, Environment Link front channel logout.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);
            var party = await tenantDataRepository.GetAsync<TrackLinkUpParty>(partyId);

            var keySequenceString = HttpContext.Request.Query[Constants.Routes.KeySequenceKey];
            var keySequence = await sequenceLogic.ValidateSequenceAsync(keySequenceString, trackName: party.ToDownTrackName);
            var keySequenceData = await sequenceLogic.ValidateKeySequenceDataAsync<TrackLinkDownSequenceData>(keySequence, party.ToDownTrackName, remove: false);
            if (!keySequenceData.KeyNames.Where(k => k == party.ToDownPartyName).Any())
            {
                throw new Exception($"Incorrect application registration key names, expected application registration name '{party.ToDownPartyName}'.");
            }

            await hrdLogic.DeleteHrdSelectionBySelectedUpPartyAsync(party.Name);
            var session = await sessionUpPartyLogic.DeleteSessionAsync(party);
            logger.ScopeTrace(() => "AuthMethod, Successful environment link front channel logout request.", triggerEvent: true);
            if (session != null)
            {
                var _ = await sessionUpPartyLogic.DeleteSessionAsync(party, session);

                if (!party.DisableSingleLogout)
                {
                    var frontChannelLogoutUri = HttpContext.GetTrackUpPartyUrl(RouteBinding.TrackName, party.Name, Constants.Routes.TrackLinkController, Constants.Endpoints.FrontChannelLogout);
                    var allowIframeOnDomains = new List<string> { frontChannelLogoutUri.UrlToDomain() };
                    (var doSingleLogout, var singleLogoutSequenceData) = await singleLogoutDownLogic.InitializeSingleLogoutAsync(new UpPartyLink { Name = party.Name, Type = party.Type }, null, session.DownPartyLinks, session.Claims, allowIframeOnDomains, hostedInIframe: true);
                    if (doSingleLogout)
                    {
                        return await singleLogoutDownLogic.StartSingleLogoutAsync(singleLogoutSequenceData);
                    }
                }
            }

            return new OkResult();
        }
    }
}
