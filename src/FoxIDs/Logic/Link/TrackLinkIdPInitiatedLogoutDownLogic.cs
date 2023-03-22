using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Sequences;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class TrackLinkIdPInitiatedLogoutDownLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly SingleLogoutDownLogic singleLogoutDownLogic;

        public TrackLinkIdPInitiatedLogoutDownLogic(TelemetryScopedLogger logger, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, SingleLogoutDownLogic singleLogoutDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.singleLogoutDownLogic = singleLogoutDownLogic;
        }     

        public async Task<IActionResult> LogoutRequestAsync(string partyId, SingleLogoutSequenceData sequenceData)
        {
            logger.ScopeTrace(() => "Down, Track link IdP initiated logout request.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantRepository.GetAsync<TrackLinkDownParty>(partyId);

            await sequenceLogic.SaveSequenceDataAsync(new TrackLinkDownSequenceData { KeyName = party.Name, Claims = sequenceData.Claims }, setKeyValidUntil: true);

            return HttpContext.GetTrackUpPartyUrl(party.ToUpTrackName, party.ToUpPartyName, Constants.Routes.TrackLinkController, Constants.Endpoints.TrackLinkIdPLogoutRequest, includeKeySequence: true).ToRedirectResult();
        }

        public async Task<IActionResult> LogoutResponseAsync(string partyId)
        {
            logger.ScopeTrace(() => "Down, Track link IdP initiated logout response.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantRepository.GetAsync<TrackLinkDownParty>(partyId);

            var keySequenceString = HttpContext.Request.Query[Constants.Routes.KeySequenceKey];
            var keySequence = await sequenceLogic.ValidateSequenceAsync(keySequenceString, trackName: party.ToUpTrackName);
            var keySequenceData = await sequenceLogic.ValidateKeySequenceDataAsync<TrackLinkUpSequenceData>(keySequence, party.ToUpTrackName);
            if (party.ToUpPartyName != keySequenceData.KeyName)
            {
                throw new Exception($"Incorrect up-party name '{keySequenceData.KeyName}', expected up-party name '{party.ToUpPartyName}'.");
            }

            await sequenceLogic.ValidateAndSetSequenceAsync(keySequenceData.DownPartySequenceString);

            return await singleLogoutDownLogic.HandleSingleLogoutAsync();
        }
    }
}
