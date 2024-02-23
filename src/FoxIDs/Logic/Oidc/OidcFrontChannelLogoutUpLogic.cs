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
        private readonly ITenantRepository tenantRepository;
        private readonly HrdLogic hrdLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly SingleLogoutDownLogic singleLogoutDownLogic;
        private readonly OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;

        public OidcFrontChannelLogoutUpLogic(TelemetryScopedLogger logger, ITenantRepository tenantRepository, HrdLogic hrdLogic, SessionUpPartyLogic sessionUpPartyLogic, SingleLogoutDownLogic singleLogoutDownLogic, OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.hrdLogic = hrdLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.singleLogoutDownLogic = singleLogoutDownLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
        }

        public async Task<IActionResult> FrontChannelLogoutAsync(string partyId)
        {
            logger.ScopeTrace(() => "AuthMethod, OIDC Front channel logout.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            var party = await tenantRepository.GetAsync<OidcUpParty>(partyId);
            logger.SetScopeProperty(Constants.Logs.UpPartyClientId, party.Client.ClientId);
            
            if (party.Client.DisableFrontChannelLogout)
            {
                return new BadRequestResult(); 
            }

            var queryDictionary = HttpContext.Request.Query.ToDictionary();
            var frontChannelLogoutRequest = queryDictionary.ToObject<FrontChannelLogoutRequest>();
            logger.ScopeTrace(() => $"AuthMethod, Front channel logout request '{frontChannelLogoutRequest.ToJsonIndented()}'.", traceType: TraceTypes.Message);
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

                var _ = await sessionUpPartyLogic.DeleteSessionAsync(party, session);
                await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsAsync(session.SessionId);

                if (!party.DisableSingleLogout)
                {
                    var allowIframeOnDomains = new List<string>().ConcatOnce(party.Client.AuthorizeUrl?.UrlToDomain()).ConcatOnce(party.Client.EndSessionUrl?.UrlToDomain()).ConcatOnce(party.Client.TokenUrl?.UrlToDomain());
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
