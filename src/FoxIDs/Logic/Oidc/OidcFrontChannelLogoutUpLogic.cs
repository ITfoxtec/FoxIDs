using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OidcFrontChannelLogoutUpLogic<TParty, TClient> : LogicBase where TParty : OidcUpParty<TClient> where TClient : OidcUpClient
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantRepository;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly SingleLogoutDownLogic singleLogoutDownLogic;

        public OidcFrontChannelLogoutUpLogic(TelemetryScopedLogger logger, ITenantRepository tenantRepository, SessionUpPartyLogic sessionUpPartyLogic, SecurityHeaderLogic securityHeaderLogic, SingleLogoutDownLogic singleLogoutDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.singleLogoutDownLogic = singleLogoutDownLogic;
        }

        public async Task<IActionResult> FrontChannelLogoutAsync(string partyId)
        {
            logger.ScopeTrace("Up, OIDC Front channel logout.");
            logger.SetScopeProperty("upPartyId", partyId);

            var party = await tenantRepository.GetAsync<OidcUpParty>(partyId);
            logger.SetScopeProperty("upPartyClientId", party.Client.ClientId);
            
            if (party.Client.DisableFrontChannelLogout)
            {
                return new BadRequestResult();
            }

            var queryDictionary = HttpContext.Request.Query.ToDictionary();
            var frontChannelLogoutRequest = queryDictionary.ToObject<FrontChannelLogoutRequest>();
            logger.ScopeTrace($"Up, Front channel logout request '{frontChannelLogoutRequest.ToJsonIndented()}'.");
            frontChannelLogoutRequest.Validate();
            if (party.Client.FrontChannelLogoutSessionRequired)
            {
                if (frontChannelLogoutRequest.SessionId.IsNullOrEmpty()) throw new ArgumentNullException(nameof(frontChannelLogoutRequest.SessionId), frontChannelLogoutRequest.GetTypeName());
            }

            var session = await sessionUpPartyLogic.DeleteSessionAsync();
            logger.ScopeTrace("Up, Successful OIDC Front channel logout request.", triggerEvent: true);
            if (session != null)
            {
                if (party.Client.FrontChannelLogoutSessionRequired)
                {
                    if (!session.Claims.Where(c => c.Claim == JwtClaimTypes.Issuer && c.Values.Where(v => v == frontChannelLogoutRequest.Issuer).Any()).Any())
                    {
                        throw new Exception("Incorrect issuer.");
                    }
                    if (session.ExternalSessionId != frontChannelLogoutRequest.SessionId)
                    {
                        throw new Exception("Incorrect session id.");
                    }
                }

                if (!party.DisableSingleLogout)
                {
                    (var doSingleLogout, var singleLogoutSequenceData) = await singleLogoutDownLogic.InitializeSingleLogoutAsync(new UpPartyLink { Name = party.Name, Type = party.Type }, null, session, redirectAfterLogout: false);
                    if (doSingleLogout)
                    {
                        securityHeaderLogic.AddAllowIframeOnUrls(new [] { party.Client.AuthorizeUrl, party.Client.EndSessionUrl });
                        return await singleLogoutDownLogic.StartSingleLogoutAsync(singleLogoutSequenceData);
                    }
                }
            }

            return new OkResult();
        }
    }
}
