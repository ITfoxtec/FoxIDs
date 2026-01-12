using FoxIDs.Infrastructure;
using FoxIDs.Logic.Tracks;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OidcFrontChannelLogoutUpLogic<TParty, TClient> : LogicSequenceBase where TParty : OidcUpParty<TClient> where TClient : OidcUpClient
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly AuditLogic auditLogic;
        private readonly HrdLogic hrdLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly SingleLogoutLogic singleLogoutLogic;
        private readonly OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;
        private readonly ActiveSessionLogic activeSessionLogic;

        public OidcFrontChannelLogoutUpLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, SecurityHeaderLogic securityHeaderLogic, AuditLogic auditLogic, HrdLogic hrdLogic, SessionUpPartyLogic sessionUpPartyLogic, SingleLogoutLogic singleLogoutLogic, OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic, ActiveSessionLogic activeSessionLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.securityHeaderLogic = securityHeaderLogic;
            this.auditLogic = auditLogic;
            this.hrdLogic = hrdLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.singleLogoutLogic = singleLogoutLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
            this.activeSessionLogic = activeSessionLogic;
        }

        public async Task<IActionResult> FrontChannelLogoutAsync(string partyId)
        {
            logger.ScopeTrace(() => "AuthMethod, OIDC Front channel logout.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);
            logger.SetScopeProperty(Constants.Logs.UpPartyType, PartyTypes.Oidc.ToString());

            var party = await tenantDataRepository.GetAsync<OidcUpParty>(partyId);
            logger.SetScopeProperty(Constants.Logs.UpPartyClientId, party.Client.ClientId);

            if (party.Client.DisableFrontChannelLogout)
            {
                return new BadRequestResult();
            }

            var queryDictionary = HttpContext.Request.Query.ToDictionary();
            var frontChannelLogoutRequest = queryDictionary.ToObject<FrontChannelLogoutRequest>();
            logger.ScopeTrace(() => $"AuthMethod, Front channel logout request '{frontChannelLogoutRequest.ToJson()}'.", traceType: TraceTypes.Message);
            frontChannelLogoutRequest.Validate(acceptEmptyIssuer: true);
            if (party.Client.FrontChannelLogoutSessionRequired)
            {
                if (frontChannelLogoutRequest.SessionId.IsNullOrEmpty()) throw new ArgumentNullException(nameof(frontChannelLogoutRequest.SessionId), frontChannelLogoutRequest.GetTypeName());
            }

            await hrdLogic.DeleteHrdSelectionBySelectedUpPartyAsync(party.Name);

            var session = await sessionUpPartyLogic.GetSessionAsync(party);
            logger.ScopeTrace(() => "AuthMethod, Successful OIDC Front channel logout request.", triggerEvent: true);
            if (session != null)
            {
                auditLogic.LogLogoutEvent(PartyTypes.Oidc, party.Id, session.SessionIdClaim);

                if (party.Client.FrontChannelLogoutSessionRequired)
                {
                    if (!party.Issuers.Where(i => i == frontChannelLogoutRequest.Issuer).Any())
                    {
                        throw new Exception("Incorrect issuer.");
                    }
                    if (session.ExternalSessionId != frontChannelLogoutRequest.SessionId)
                    {
                        throw new Exception("Incorrect session id.");
                    }
                }

                _ = await sessionUpPartyLogic.DeleteSessionAsync(party, session);
                await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsBySessionIdAsync(session.SessionIdClaim);
                await activeSessionLogic.DeleteSessionAsync(session.SessionIdClaim);

                if (party.DisableSingleLogout)
                {
                    await sessionUpPartyLogic.DeleteSessionTrackCookieGroupAsync(party);
                }
                else
                {
                    var allowIframeOnDomains = new List<string>().ConcatOnce(party.Client.AuthorizeUrl?.UrlToDomain()).ConcatOnce(party.Client.EndSessionUrl?.UrlToDomain()).ConcatOnce(party.Client.TokenUrl?.UrlToDomain());
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
