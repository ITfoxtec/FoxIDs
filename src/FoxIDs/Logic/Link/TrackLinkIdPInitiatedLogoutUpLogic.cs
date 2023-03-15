using FoxIDs.Infrastructure;
using FoxIDs.Logic.Tracks;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;
using FoxIDs.Models.Session;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class TrackLinkIdPInitiatedLogoutUpLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly TrackIssuerLogic trackIssuerLogic;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly HrdLogic hrdLogic;
        private readonly SingleLogoutDownLogic singleLogoutDownLogic;

        public TrackLinkIdPInitiatedLogoutUpLogic(TelemetryScopedLogger logger, TrackIssuerLogic trackIssuerLogic, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, SessionUpPartyLogic sessionUpPartyLogic, HrdLogic hrdLogic, SingleLogoutDownLogic singleLogoutDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.trackIssuerLogic = trackIssuerLogic;
            this.serviceProvider = serviceProvider;
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

            await hrdLogic.DeleteHrdSelectionBySelectedUpPartyAsync(party.Name);
            var session = await sessionUpPartyLogic.DeleteSessionAsync(party);

            if (party.DisableSingleLogout)
            {
                return await LogoutRequestAsync(party, keySequenceString);
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
                    return await LogoutRequestAsync(party, keySequenceString);
                }
            }
        }


        public async Task<IActionResult> LogoutRequestAsync(TrackLinkUpParty party, string keySequenceString)
        {
            await sequenceLogic.SaveSequenceDataAsync(new TrackLinkUpSequenceData { KeyName = party.Name, DownPartySequenceString = keySequenceString }, setKeyValidUntil: true);

            return HttpContext.GetTrackDownPartyUrl(party.ToDownTrackName, party.ToDownPartyName, party.SelectedUpParties, Constants.Routes.TrackLinkController, Constants.Endpoints.TrackLinkIdPLogoutResponse, includeKeySequence: true).ToRedirectResult();
        }
    }
}
