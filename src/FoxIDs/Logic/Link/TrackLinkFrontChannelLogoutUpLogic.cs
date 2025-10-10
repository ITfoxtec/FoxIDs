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
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly SequenceLogic sequenceLogic;
        private readonly AuditLogic auditLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly HrdLogic hrdLogic;
        private readonly SingleLogoutLogic singleLogoutLogic;
        private readonly OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;

        public TrackLinkFrontChannelLogoutUpLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, SecurityHeaderLogic securityHeaderLogic, SequenceLogic sequenceLogic, AuditLogic auditLogic, SessionUpPartyLogic sessionUpPartyLogic, HrdLogic hrdLogic, SingleLogoutLogic singleLogoutLogic, OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.securityHeaderLogic = securityHeaderLogic;
            this.sequenceLogic = sequenceLogic;
            this.auditLogic = auditLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.hrdLogic = hrdLogic;
            this.singleLogoutLogic = singleLogoutLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
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
            await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsBySessionIdAsync(session.SessionIdClaim);
            logger.ScopeTrace(() => "AuthMethod, Successful environment link front channel logout request.", triggerEvent: true);

            auditLogic.LogLogoutEvent(PartyTypes.TrackLink, party.Id, session.SessionIdClaim);

            if (session != null)
            {
                if (party.DisableSingleLogout)
                {
                    await sessionUpPartyLogic.DeleteSessionTrackCookieGroupAsync(party);
                }
                else
                {
                    var frontChannelLogoutUri = HttpContext.GetTrackUpPartyUrl(RouteBinding.TrackName, party.Name, Constants.Routes.TrackLinkController, Constants.Endpoints.FrontChannelLogout);
                    var allowIframeOnDomains = new List<string> { frontChannelLogoutUri.UrlToDomain() };
                    (var doSingleLogout, var singleLogoutSequenceData) = await singleLogoutLogic.InitializeSingleLogoutAsync(party, null, allowIframeOnDomains: allowIframeOnDomains, hostedInIframe: true);
                    if (doSingleLogout)
                    {
                        securityHeaderLogic.AddFrameSrcAllowAll();
                        return await singleLogoutLogic.StartSingleLogoutAsync(singleLogoutSequenceData);
                    }
                }
            }

            return new OkResult();
        }
    }
}
