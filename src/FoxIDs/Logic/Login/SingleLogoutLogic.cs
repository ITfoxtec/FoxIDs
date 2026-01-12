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
using System.Collections.Generic;
using FoxIDs.Repository;
using MongoDB.Driver;
using FoxIDs.Models.Logic;
using ITfoxtec.Identity;

namespace FoxIDs.Logic
{
    public class SingleLogoutLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;

        public SingleLogoutLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantDataRepository tenantDataRepository, SequenceLogic sequenceLogic, SessionUpPartyLogic sessionUpPartyLogic, SecurityHeaderLogic securityHeaderLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.sequenceLogic = sequenceLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.securityHeaderLogic = securityHeaderLogic;
        }

        public async Task<(bool, SingleLogoutSequenceData)> InitializeSingleLogoutAsync(UpParty upParty, DownPartySessionLink initiatingDownParty, UpSequenceData upSequenceData = null, IEnumerable<string> allowIframeOnDomains = null, bool hostedInIframe = false)
        {
            logger.ScopeTrace(() => "Initialize single logout.");

            var sessionTrackCookieGroup = await sessionUpPartyLogic.GetAndDeleteSessionTrackCookieGroupAsync(upParty);

            var upPartyLinks = sessionTrackCookieGroup?.UpPartyLinks?.Where(p => p.Id != upParty.Id);
            var downPartyLinks = sessionTrackCookieGroup?.DownPartyLinks?.Where(p => p.SupportSingleLogout && initiatingDownParty == null || p.Id != initiatingDownParty?.Id);
            if (sessionTrackCookieGroup?.UpPartyLinks == null || !(upPartyLinks?.Count() > 0) && (sessionTrackCookieGroup?.DownPartyLinks == null || !(downPartyLinks?.Count() > 0) || !(sessionTrackCookieGroup?.Claims?.Count() > 0)))
            {
                return (false, null);
            }

            var sequenceData = new SingleLogoutSequenceData
            {
                UpPartyId = upParty.Id,
                UpPartyType = upParty.Type,
                SessionId = sessionTrackCookieGroup.Claims?.FindFirstOrDefaultValue(c => c.Claim == JwtClaimTypes.SessionId),
                Claims = sessionTrackCookieGroup.Claims,
                DownPartyLinks = downPartyLinks,
                UpPartyLinks = upPartyLinks,
                DownPartyLink = upSequenceData?.DownPartyLink,
                HostedInIframe = hostedInIframe,
                AllowIframeOnDomains = allowIframeOnDomains
            };

            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
            return (true, sequenceData);
        }

        public async Task<IActionResult> StartSingleLogoutAsync(SingleLogoutSequenceData sequenceData)
        {
            logger.ScopeTrace(() => "Start single logout.");
            if (sequenceData.UpPartyLinks?.Count() > 0)
            {
                return await HandleSingleLogoutUpAsync(sequenceData);
            }
            else
            {
                return await HandleSingleLogoutDownAsync(sequenceData);
            }
        }

        public async Task<IActionResult> HandleSingleLogoutUpAsync(SingleLogoutSequenceData sequenceData = null)
        {
            sequenceData = sequenceData ?? await sequenceLogic.GetSequenceDataAsync<SingleLogoutSequenceData>(remove: false);
            if (sequenceData.HostedInIframe && sequenceData.AllowIframeOnDomains?.Count() > 0)
            {
                securityHeaderLogic.AddAllowIframeOnDomains(sequenceData.AllowIframeOnDomains);
            }

            if (sequenceData.UpPartyLinks?.Count() > 0)
            {
                var loginUpPartySessionLink = sequenceData.UpPartyLinks.Where(p => p.Type == PartyTypes.Login).FirstOrDefault();
                if (loginUpPartySessionLink != null)
                {
                    sequenceData.UpPartyLinks = sequenceData.UpPartyLinks.Where(p => p.Id != loginUpPartySessionLink.Id);
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return await serviceProvider.GetService<LogoutUpLogic>().LogoutRedirect(GetUpPartyLink(loginUpPartySessionLink), GetLoginRequest(sequenceData), isSingleLogout: true);
                }

                var oidcUpPartySessionLink = sequenceData.UpPartyLinks.Where(p => p.Type == PartyTypes.Oidc).FirstOrDefault();
                if (oidcUpPartySessionLink != null)
                {
                    sequenceData.UpPartyLinks = sequenceData.UpPartyLinks.Where(p => p.Id != oidcUpPartySessionLink.Id);
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return await serviceProvider.GetService<OidcRpInitiatedLogoutUpLogic<OidcUpParty, OidcUpClient>>().EndSessionRequestRedirectAsync(GetUpPartyLink(oidcUpPartySessionLink), GetLoginRequest(sequenceData), isSingleLogout: true);
                }

                var samlUpPartySessionLink = sequenceData.UpPartyLinks.Where(p => p.Type == PartyTypes.Saml2).FirstOrDefault();
                if (samlUpPartySessionLink != null)
                {
                    sequenceData.UpPartyLinks = sequenceData.UpPartyLinks.Where(p => p.Id != samlUpPartySessionLink.Id);
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return await serviceProvider.GetService<SamlLogoutUpLogic>().LogoutRequestRedirectAsync(GetUpPartyLink(samlUpPartySessionLink), GetLoginRequest(sequenceData), isSingleLogout: true);
                }

                var trackLinkUpPartySessionLink = sequenceData.UpPartyLinks.Where(p => p.Type == PartyTypes.TrackLink).FirstOrDefault();
                if (trackLinkUpPartySessionLink != null)
                {
                    sequenceData.UpPartyLinks = sequenceData.UpPartyLinks.Where(p => p.Id != trackLinkUpPartySessionLink.Id);
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return await serviceProvider.GetService<TrackLinkRpInitiatedLogoutUpLogic>().LogoutRequestRedirectAsync(GetUpPartyLink(trackLinkUpPartySessionLink), GetLoginRequest(sequenceData), isSingleLogout: true);
                }

                var externalLoginUpPartySessionLink = sequenceData.UpPartyLinks.Where(p => p.Type == PartyTypes.ExternalLogin).FirstOrDefault();
                if (externalLoginUpPartySessionLink != null)
                {
                    sequenceData.UpPartyLinks = sequenceData.UpPartyLinks.Where(p => p.Id != externalLoginUpPartySessionLink.Id);
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return await serviceProvider.GetService<ExternalLogoutUpLogic>().LogoutRedirect(GetUpPartyLink(externalLoginUpPartySessionLink), GetLoginRequest(sequenceData), isSingleLogout: true);
                }

                throw new InvalidOperationException($"Unknown authentication method type '{sequenceData.UpPartyLinks.FirstOrDefault()?.Type}' in single logout.");
            }

            return await HandleSingleLogoutDownAsync(sequenceData);
        }

        private LogoutRequest GetLoginRequest(SingleLogoutSequenceData sequenceData)
        {
            if (sequenceData?.DownPartyLink != null)
            {
                return new LogoutRequest
                {
                    DownPartyLink = sequenceData.DownPartyLink,
                    SessionId = sequenceData.SessionId,
                    PostLogoutRedirect = true,
                };
            }
            return null;
        }

        private UpPartyLink GetUpPartyLink(UpPartySessionLink upPartySessionLink) => new UpPartyLink { Name = upPartySessionLink.Id.PartyIdToName(), Type = upPartySessionLink.Type };

        public async Task<IActionResult> HandleSingleLogoutDownAsync(SingleLogoutSequenceData sequenceData = null)
        {
            sequenceData = sequenceData ?? await sequenceLogic.GetSequenceDataAsync<SingleLogoutSequenceData>(remove: false);
            if (sequenceData.HostedInIframe && sequenceData.AllowIframeOnDomains?.Count() > 0)
            {
                securityHeaderLogic.AddAllowIframeOnDomains(sequenceData.AllowIframeOnDomains);
            }

            if (sequenceData.DownPartyLinks?.Count() > 0)
            {
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

                throw new InvalidOperationException($"Unknown down-party type '{sequenceData.DownPartyLinks.FirstOrDefault()?.Type}' in single logout.");
            }

            await sequenceLogic.RemoveSequenceDataAsync<SingleLogoutSequenceData>();
            logger.ScopeTrace(() => "Successful Single Logout.", triggerEvent: true);

            if (sequenceData.HostedInIframe)
            {
                return new OkResult();
            }
            else
            {
                return await ResponseUpPartyAsync(sequenceData.UpPartyId, sequenceData.UpPartyType);
            }
        }

        private async Task<IActionResult> ResponseUpPartyAsync(string partyId, PartyTypes upPartyType)
        {
            logger.ScopeTrace(() => $"Single Logout response, Authentication type {upPartyType}.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);
            logger.SetScopeProperty(Constants.Logs.UpPartyType, PartyTypes.Login.ToString());

            switch (upPartyType)
            {
                case PartyTypes.Login:
                    return HttpContext.GetUpPartyUrl(partyId.PartyIdToName(), Constants.Routes.LoginController, Constants.Endpoints.SingleLogoutDone, includeSequence: true).ToRedirectResult();
                case PartyTypes.Oidc:
                    var oidcUpParty = await tenantDataRepository.GetAsync<UpParty>(partyId);
                    return HttpContext.GetUpPartyUrl(partyId.PartyIdToName(), Constants.Routes.OAuthController, Constants.Endpoints.SingleLogoutDone, includeSequence: true, partyBindingPattern: oidcUpParty.PartyBindingPattern).ToRedirectResult();
                case PartyTypes.Saml2:
                    var samlUpParty = await tenantDataRepository.GetAsync<UpParty>(partyId);
                    return HttpContext.GetUpPartyUrl(partyId.PartyIdToName(), Constants.Routes.SamlController, Constants.Endpoints.SingleLogoutDone, includeSequence: true, partyBindingPattern: samlUpParty.PartyBindingPattern).ToRedirectResult();
                case PartyTypes.TrackLink:
                    return HttpContext.GetUpPartyUrl(partyId.PartyIdToName(), Constants.Routes.TrackLinkController, Constants.Endpoints.SingleLogoutDone, includeSequence: true).ToRedirectResult();

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
