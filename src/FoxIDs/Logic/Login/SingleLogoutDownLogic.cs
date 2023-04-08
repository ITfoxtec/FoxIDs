using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Sequences;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using FoxIDs.Models.Session;
using ITfoxtec.Identity;
using System.Collections.Generic;
using FoxIDs.Repository;

namespace FoxIDs.Logic
{
    public class SingleLogoutDownLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;

        public SingleLogoutDownLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, SecurityHeaderLogic securityHeaderLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.securityHeaderLogic = securityHeaderLogic;
        }

        public async Task<(bool, SingleLogoutSequenceData)> InitializeSingleLogoutAsync(UpPartyLink upPartyLink, DownPartySessionLink initiatingDownParty, IEnumerable<DownPartySessionLink> downPartyLinks, IEnumerable<ClaimAndValues> claims, IEnumerable<string> allowIframeOnDomains = null, bool hostedInIframe = false)
        {
            logger.ScopeTrace(() => "Initialize single logout.");

            downPartyLinks = downPartyLinks?.Where(p => p.SupportSingleLogout && (initiatingDownParty == null || p.Id != initiatingDownParty.Id));
            if (!(downPartyLinks?.Count() > 0) || !(claims?.Count() > 0))
            {
                return (false, null);
            }

            var sequenceData = new SingleLogoutSequenceData
            {
                UpPartyName = upPartyLink.Name,
                UpPartyType = upPartyLink.Type,
                DownPartyLinks = downPartyLinks,
                HostedInIframe = hostedInIframe,
                AllowIframeOnDomains = allowIframeOnDomains
            };

            if (downPartyLinks.Where(p => p.Type == PartyTypes.Saml2).Any())
            {
                sequenceData.Claims = claims;
            }
            else
            {
                sequenceData.Claims = claims.Where(c => c.Claim == JwtClaimTypes.SessionId);
            }

            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
            return (true, sequenceData);
        }

        public async Task<IActionResult> StartSingleLogoutAsync(SingleLogoutSequenceData sequenceData)
        {
            logger.ScopeTrace(() => "Start single logout.");
            return await HandleSingleLogoutAsync(sequenceData);
        }
      
        public async Task<IActionResult> HandleSingleLogoutAsync(SingleLogoutSequenceData sequenceData = null)
        {
            sequenceData = sequenceData ?? await sequenceLogic.GetSequenceDataAsync<SingleLogoutSequenceData>(remove: false);
            if (sequenceData.HostedInIframe && sequenceData.AllowIframeOnDomains?.Count() > 0)
            {
                securityHeaderLogic.AddAllowIframeOnDomains(sequenceData.AllowIframeOnDomains);
            }

            var samlDownPartyId = sequenceData.DownPartyLinks.Where(p => p.Type == PartyTypes.Saml2).Select(p => p.Id).FirstOrDefault();
            var doSamlLogoutInIframe = sequenceData.HostedInIframe && samlDownPartyId != null;

            var oidcDownPartyIds = sequenceData.DownPartyLinks.Where(p => p.Type == PartyTypes.Oidc).Select(p => p.Id);
            if (oidcDownPartyIds.Count() > 0)
            {
                sequenceData.DownPartyLinks = sequenceData.DownPartyLinks.Where(p => p.Type != PartyTypes.Oidc);
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return await serviceProvider.GetService<OidcFrontChannelLogoutDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().LogoutRequestAsync(oidcDownPartyIds, sequenceData, sequenceData.HostedInIframe, doSamlLogoutInIframe);
            }

            var trackLinkDownPartyIds = sequenceData.DownPartyLinks.Where(p => p.Type == PartyTypes.TrackLink).Select(p => p.Id);
            if (trackLinkDownPartyIds.Count() > 0)
            {
                sequenceData.DownPartyLinks = sequenceData.DownPartyLinks.Where(p => p.Type != PartyTypes.TrackLink);
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return await serviceProvider.GetService<TrackLinkFrontChannelLogoutDownLogic>().LogoutRequestAsync(trackLinkDownPartyIds, sequenceData, sequenceData.HostedInIframe, doSamlLogoutInIframe);
            }

            if (samlDownPartyId != null)
            {
                sequenceData.DownPartyLinks = sequenceData.DownPartyLinks.Where(p => p.Id != samlDownPartyId);
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return await serviceProvider.GetService<SamlLogoutDownLogic>().SingleLogoutRequestAsync(samlDownPartyId, sequenceData);
            }

            await sequenceLogic.RemoveSequenceDataAsync<SingleLogoutSequenceData>();
            logger.ScopeTrace(() => "Successful Single Logout.", triggerEvent: true);

            if (sequenceData.HostedInIframe)
            {
                return new OkResult();
            }
            else
            {
                return await ResponseUpPartyAsync(sequenceData.UpPartyName, sequenceData.UpPartyType);
            }
        }

        private async Task<IActionResult> ResponseUpPartyAsync(string upPartyName, PartyTypes upPartyType)
        {
            logger.ScopeTrace(() => $"Single Logout response, Up type {upPartyType}.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, upPartyName);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            switch (upPartyType)
            {
                case PartyTypes.Login:
                    return HttpContext.GetUpPartyUrl(upPartyName, Constants.Routes.LoginController, Constants.Endpoints.SingleLogoutDone, includeSequence: true).ToRedirectResult();
                case PartyTypes.Oidc:
                    var oidcUpParty = await tenantRepository.GetAsync<UpParty>(partyId);
                    return HttpContext.GetUpPartyUrl(upPartyName, Constants.Routes.OAuthController, Constants.Endpoints.SingleLogoutDone, includeSequence: true, partyBindingPattern: oidcUpParty.PartyBindingPattern).ToRedirectResult();
                case PartyTypes.Saml2:
                    var samlUpParty = await tenantRepository.GetAsync<UpParty>(partyId);
                    return HttpContext.GetUpPartyUrl(upPartyName, Constants.Routes.SamlController, Constants.Endpoints.SingleLogoutDone, includeSequence: true, partyBindingPattern: samlUpParty.PartyBindingPattern).ToRedirectResult();
                case PartyTypes.TrackLink:
                    return HttpContext.GetUpPartyUrl(upPartyName, Constants.Routes.TrackLinkController, Constants.Endpoints.SingleLogoutDone, includeSequence: true).ToRedirectResult();

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
