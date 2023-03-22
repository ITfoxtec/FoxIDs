using FoxIDs.Infrastructure;
using FoxIDs.Logic.Tracks;
using FoxIDs.Models;
using FoxIDs.Models.Sequences;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class TrackLinkIdPInitiatedLogoutUpLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly HrdLogic hrdLogic;
        private readonly SingleLogoutDownLogic singleLogoutDownLogic;

        public TrackLinkIdPInitiatedLogoutUpLogic(TelemetryScopedLogger logger, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, SessionUpPartyLogic sessionUpPartyLogic, HrdLogic hrdLogic, SingleLogoutDownLogic singleLogoutDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.hrdLogic = hrdLogic;
            this.singleLogoutDownLogic = singleLogoutDownLogic;
        }

        public async Task<IActionResult> LogoutRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "Up, Track link IdP initiated logout request.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);
            var party = await tenantRepository.GetAsync<TrackLinkUpParty>(partyId);

            var keySequenceString = HttpContext.Request.Query[Constants.Routes.KeySequenceKey];
            var keySequence = await sequenceLogic.ValidateSequenceAsync(keySequenceString, trackName: party.ToDownTrackName);
            var keySequenceData = await sequenceLogic.ValidateKeySequenceDataAsync<TrackLinkDownSequenceData>(keySequence, party.ToDownTrackName, remove: false);
            if (party.ToDownPartyName != keySequenceData.KeyName)
            {
                throw new Exception($"Incorrect down-party name '{keySequenceData.KeyName}', expected down-party name '{party.ToDownPartyName}'.");
            }

            var sequenceData = new TrackLinkUpSequenceData 
            {
                KeyName = party.Name,
                DownPartySequenceString = keySequenceString,
                ExternalInitiatedSingleLogout = true,
                UpPartyId = party.Id
            };
            await sequenceLogic.SaveSequenceDataAsync(sequenceData);

            await hrdLogic.DeleteHrdSelectionBySelectedUpPartyAsync(party.Name);
            var session = await sessionUpPartyLogic.DeleteSessionAsync(party);

            if (party.DisableSingleLogout)
            {
                return await LogoutResponseAsync(party, sequenceData);
            }
            else
            {
                (var doSingleLogout, var singleLogoutSequenceData) = await singleLogoutDownLogic.InitializeSingleLogoutAsync(new UpPartyLink { Name = party.Name, Type = party.Type }, null, session.DownPartyLinks, session.Claims);

                if (doSingleLogout)
                {
                    return await singleLogoutDownLogic.StartSingleLogoutAsync(singleLogoutSequenceData);
                }
                else
                {
                    return await LogoutResponseAsync(party, sequenceData);
                }
            }
        }

        public async Task<IActionResult> LogoutResponseAsync(string partyId, TrackLinkUpSequenceData sequenceData)
        {
            var party = await tenantRepository.GetAsync<TrackLinkUpParty>(partyId);
            return await LogoutResponseAsync(party, sequenceData);
        }

        public async Task<IActionResult> LogoutResponseAsync(TrackLinkUpParty party, TrackLinkUpSequenceData sequenceData)
        {
            await sequenceLogic.SaveSequenceDataAsync(sequenceData, setKeyValidUntil: true);

            return HttpContext.GetTrackDownPartyUrl(party.ToDownTrackName, party.ToDownPartyName, party.SelectedUpParties, Constants.Routes.TrackLinkController, Constants.Endpoints.TrackLinkIdPLogoutResponse, includeKeySequence: true).ToRedirectResult();
        }
    }
}
