﻿using System;
using System.Linq;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using FoxIDs.Models.Sequences;
using Microsoft.AspNetCore.WebUtilities;

namespace FoxIDs.Logic
{
    public class OidcFrontChannelLogoutDownLogic<TParty, TClient, TScope, TClaim> : LogicSequenceBase where TParty : OidcDownParty<TClient, TScope, TClaim> where TClient : OidcDownClient<TScope, TClaim> where TScope : OidcDownScope<TClaim> where TClaim : OidcDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly TrackIssuerLogic trackIssuerLogic;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly SingleLogoutLogic singleLogoutLogic;

        public OidcFrontChannelLogoutDownLogic(TelemetryScopedLogger logger, TrackIssuerLogic trackIssuerLogic, ITenantDataRepository tenantDataRepository, SecurityHeaderLogic securityHeaderLogic, SingleLogoutLogic singleLogoutLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.trackIssuerLogic = trackIssuerLogic;
            this.tenantDataRepository = tenantDataRepository;
            this.securityHeaderLogic = securityHeaderLogic;
            this.singleLogoutLogic = singleLogoutLogic;
        }

        public async Task<IActionResult> LogoutRequestAsync(IEnumerable<string> partyIds, SingleLogoutSequenceData sequenceData, bool hostedInIframe, bool doSamlLogoutInIframe)
        {
            logger.ScopeTrace(() => "AppReg, OIDC Front Channel logout request.");
            var frontChannelLogoutRequest = new FrontChannelLogoutRequest
            {
                Issuer = trackIssuerLogic.GetIssuer(),
                SessionId = sequenceData.Claims.FindFirstOrDefaultValue(c => c.Claim == JwtClaimTypes.SessionId)
            };
            var nameValueCollection = frontChannelLogoutRequest.ToDictionary();

            TParty firstParty = null;
            var partyLogoutUrls = new List<string>();
            foreach (var partyId in partyIds)
            {
                try
                {
                    var party = await tenantDataRepository.GetAsync<TParty>(partyId);
                    if (party.Client == null)
                    {
                        throw new NotSupportedException("Application Client not configured.");
                    }
                    if (party.Client.FrontChannelLogoutUri.IsNullOrWhiteSpace())
                    {
                        throw new Exception("Front channel logout URI not configured.");
                    }

                    firstParty = party;
                    if (party.Client.FrontChannelLogoutSessionRequired)
                    {
                        partyLogoutUrls.Add(QueryHelpers.AddQueryString(party.Client.FrontChannelLogoutUri, nameValueCollection));
                    }
                    else
                    {
                        partyLogoutUrls.Add(party.Client.FrontChannelLogoutUri);
                    }
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, $"Unable to get front channel logout for application ID '{partyId}'.");
                }
            }

            if (partyLogoutUrls.Count() <= 0 || firstParty == null)
            {
                throw new InvalidOperationException("Unable to complete front channel logout. Please close the browser to logout.");
            }

            if (doSamlLogoutInIframe)
            {
                securityHeaderLogic.AddFrameSrcAllowAll();
                // Start SAML logout
                partyLogoutUrls.Add(GetFrontChannelLogoutDoneUrl(sequenceData, firstParty));
            }
            else
            {
                securityHeaderLogic.AddFrameSrcUrls(partyLogoutUrls);
            }
            string redirectUrl = hostedInIframe ? null : GetFrontChannelLogoutDoneUrl(sequenceData, firstParty);
            return partyLogoutUrls.ToHtmIframePage(redirectUrl, RouteBinding.DisplayName ?? "FoxIDs").ToContentResult();
        }

        private string GetFrontChannelLogoutDoneUrl(SingleLogoutSequenceData sequenceData, TParty firstParty)
        {
            return HttpContext.GetDownPartyUrl(firstParty.Name, sequenceData.UpPartyId.PartyIdToName(), Constants.Routes.OAuthController, Constants.Endpoints.FrontChannelLogoutDone, includeSequence: true, firstParty.PartyBindingPattern);
        }

        public Task<IActionResult> LogoutDoneAsync()
        {
            return singleLogoutLogic.HandleSingleLogoutDownAsync();
        }
    }
}
