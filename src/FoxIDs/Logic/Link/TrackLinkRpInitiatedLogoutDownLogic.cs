using FoxIDs.Infrastructure;
using FoxIDs.Logic.Tracks;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;
using FoxIDs.Models.Session;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class TrackLinkRpInitiatedLogoutDownLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly HrdLogic hrdLogic;

        public TrackLinkRpInitiatedLogoutDownLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, HrdLogic hrdLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.hrdLogic = hrdLogic;
        }

        public async Task<IActionResult> LogoutRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "Down, Track link RP initiated logout request.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantRepository.GetAsync<TrackLinkDownParty>(partyId);

            var keySequenceString = HttpContext.Request.Query[Constants.Routes.KeySequenceKey];
            var keySequence = await sequenceLogic.ValidateSequenceAsync(keySequenceString, trackName: party.ToUpTrackName);
            var keySequenceData = await sequenceLogic.ValidateKeySequenceDataAsync<TrackLinkUpSequenceData>(keySequence, party.ToUpTrackName, remove: false);
            if (party.ToUpPartyName != keySequenceData.KeyName)
            {
                throw new Exception($"Incorrect up-party name '{keySequenceData.KeyName}', expected up-party name '{party.ToUpPartyName}'.");
            }

            await sequenceLogic.SaveSequenceDataAsync(new TrackLinkDownSequenceData { KeyName = party.Name, UpPartySequenceString = keySequenceString });

            var toUpParty = await hrdLogic.GetUpPartyAndDeleteHrdSelectionAsync();
            logger.ScopeTrace(() => $"Request, Up type '{toUpParty.Type}'.");
            switch (toUpParty.Type)
            {
                case PartyTypes.Login:
                    return await serviceProvider.GetService<LogoutUpLogic>().LogoutRedirect(toUpParty, GetLogoutRequest(party, keySequenceData.SessionId, keySequenceData.RequireLogoutConsent));
                case PartyTypes.OAuth2:
                    throw new NotImplementedException();
                case PartyTypes.Oidc:
                    return await serviceProvider.GetService<OidcRpInitiatedLogoutUpLogic<OidcUpParty, OidcUpClient>>().EndSessionRequestRedirectAsync(toUpParty, GetLogoutRequest(party, keySequenceData.SessionId, keySequenceData.RequireLogoutConsent));
                case PartyTypes.Saml2:
                    return await serviceProvider.GetService<SamlLogoutUpLogic>().LogoutRequestRedirectAsync(toUpParty, GetLogoutRequest(party, keySequenceData.SessionId, keySequenceData.RequireLogoutConsent));
                case PartyTypes.TrackLink:
                    return await serviceProvider.GetService<TrackLinkRpInitiatedLogoutUpLogic>().LogoutRequestRedirectAsync(toUpParty, GetLogoutRequest(party, keySequenceData.SessionId, keySequenceData.RequireLogoutConsent));

                default:
                    throw new NotSupportedException($"Party type '{toUpParty.Type}' not supported.");
            }
        }

        private LogoutRequest GetLogoutRequest(TrackLinkDownParty party, string sessionId, bool requireLogoutConsent)
        {
            var logoutRequest = new LogoutRequest
            {
                DownPartyLink = new DownPartySessionLink { SupportSingleLogout = true, Id = party.Id, Type = party.Type },
                SessionId = sessionId,
                RequireLogoutConsent = requireLogoutConsent,
                PostLogoutRedirect = true
            };

            return logoutRequest;
        }

        public async Task<IActionResult> LogoutResponseAsync(string partyId)
        {
            logger.ScopeTrace(() => "Down, Track link RP initiated logout response.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantRepository.GetAsync<TrackLinkDownParty>(partyId);

            var sequenceData = await sequenceLogic.GetSequenceDataAsync<TrackLinkDownSequenceData>(remove: false);
            await sequenceLogic.SaveSequenceDataAsync(sequenceData, setKeyValidUntil: true);

            return HttpContext.GetTrackUpPartyUrl(party.ToUpTrackName, party.ToUpPartyName, Constants.Routes.TrackLinkController, Constants.Endpoints.TrackLinkRpLogoutResponse, includeKeySequence: true).ToRedirectResult();
        }
    }
}
