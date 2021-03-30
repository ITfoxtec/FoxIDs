using System;
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
    public class OidcFrontChannelLogoutDownLogic<TParty, TClient, TScope, TClaim> : LogicBase where TParty : OidcDownParty<TClient, TScope, TClaim> where TClient : OidcDownClient<TScope, TClaim> where TScope : OidcDownScope<TClaim> where TClaim : OidcDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly TrackIssuerLogic trackIssuerLogic;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;

        public OidcFrontChannelLogoutDownLogic(TelemetryScopedLogger logger, TrackIssuerLogic trackIssuerLogic, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, SecurityHeaderLogic securityHeaderLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.trackIssuerLogic = trackIssuerLogic;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.securityHeaderLogic = securityHeaderLogic;
        }

        public async Task<IActionResult> SingleLogoutRequestAsync(IEnumerable<string> partyIds, SingleLogoutSequenceData sequenceData)
        {
            var frontChannelLogoutRequest = new FrontChannelLogoutRequest
            {
                Issuer = trackIssuerLogic.GetIssuer(),
                SessionId = sequenceData.Claims.FindFirstValue(c => c.Claim == JwtClaimTypes.SessionId)
            };
            var nameValueCollection = frontChannelLogoutRequest.ToDictionary();

            var partyLogoutUrls = new List<string>();
            foreach (var partyId in partyIds)
            {
                try
                {
                    var party = await tenantRepository.GetAsync<TParty>(partyId);
                    if (party.Client == null)
                    {
                        throw new NotSupportedException("Party Client not configured.");
                    }
                    if (party.Client.FrontChannelLogoutUri.IsNullOrWhiteSpace())
                    {
                        throw new Exception("Front channel logout URI not configured.");
                    }

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
                    logger.Warning(ex, $"Unable to get front channel logout for party ID '{partyId}'.");
                }
            }

            if (partyLogoutUrls.Count() > 0)
            {

            }
            else
            {
                throw new NotImplementedException();
            }

            securityHeaderLogic.AddFrameSrc(partyLogoutUrls);
            await securityHeaderLogic.RemoveFormActionSequenceDataAsync();
            return string.Concat(HtmIframePageList(partyLogoutUrls, "FoxIDs")).ToContentResult();
        }

        public static IEnumerable<string> HtmIframePageList(List<string> urls, string redirectUrl, string title = "OAuth 2.0")
        {
            yield return
$@"<!DOCTYPE html>
<html lang=""en"">
    <head>
        <meta charset=""utf-8"" />
        <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
        <title>{title}</title>
    </head>
    <body onload=""alert('page loaded')"">
        <div>
";
            if (urls?.Count > 0)
            {
                foreach (var url in urls)
                {
                    yield return
$@"            <iframe width=""0"" height=""0"" frameborder=""0"" src=""{url}""></iframe>
";
                }
            }

            yield return
$@"        </div>
    </body>
</html>";
        }
    }
}
