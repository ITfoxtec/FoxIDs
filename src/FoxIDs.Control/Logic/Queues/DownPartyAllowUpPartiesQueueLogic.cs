using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Queue;
using FoxIDs.Models;
using FoxIDs.Models.Queue;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class DownPartyAllowUpPartiesQueueLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly BackgroundQueue backgroundQueue;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly DownPartyCacheLogic downPartyCacheLogic;

        public DownPartyAllowUpPartiesQueueLogic(TelemetryScopedLogger logger, BackgroundQueue backgroundQueue, ITenantDataRepository tenantDataRepository, DownPartyCacheLogic downPartyCacheLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.backgroundQueue = backgroundQueue;
            this.tenantDataRepository = tenantDataRepository;
            this.downPartyCacheLogic = downPartyCacheLogic;
        }

        public async Task UpdateUpParty(UpParty oldUpParty, UpParty newUpParty)
        {
            if (UpPartyHrdHasChanged(oldUpParty, newUpParty))
            {
                await AddToQueue(newUpParty, false);
            }
        }

        private bool UpPartyHrdHasChanged(UpParty oldUpParty, UpParty newUpParty)
        {
            if (oldUpParty == null)
            {
                return true;
            }

            if (oldUpParty.DisplayName != newUpParty.DisplayName)
            {
                return true;
            }

            var oldHrdIssuers = oldUpParty.ReadIssuers != null ? string.Join(',', oldUpParty.ReadIssuers) : string.Empty;
            var newHrdIssuers = newUpParty.ReadIssuers != null ? string.Join(',', newUpParty.ReadIssuers) : string.Empty;
            if (oldHrdIssuers != newHrdIssuers) 
            {
                return true;
            }

            if (oldUpParty.SpIssuer != newUpParty.SpIssuer)
            {
                return true;
            }

            var oldHrdDomains = oldUpParty.HrdDomains != null ? string.Join(',', oldUpParty.HrdDomains) : string.Empty;
            var newHrdDomains = newUpParty.HrdDomains != null ? string.Join(',', newUpParty.HrdDomains) : string.Empty;
            if (oldHrdDomains != newHrdDomains)
            {
                return true;
            }

            if (oldUpParty.HrdShowButtonWithDomain != newUpParty.HrdShowButtonWithDomain)
            {
                return true;
            }

            if (oldUpParty.HrdDisplayName != newUpParty.HrdDisplayName)
            {
                return true;
            }

            if (oldUpParty.HrdLogoUrl != newUpParty.HrdLogoUrl)
            {
                return true;
            }

            return false;
        }

        public async Task DeleteUpParty(string upPartyName)
        {
            await AddToQueue(new UpParty { Name = upPartyName }, true);
        }

        private async Task AddToQueue(UpParty upParty, bool remove)
        {
            var message = new UpPartyHrdQueueMessage
            {
                Name = upParty.Name,
                DisplayName = upParty.DisplayName,
                Issuers = upParty.ReadIssuers,
                SpIssuer = upParty.SpIssuer,
                HrdDisplayName = upParty.HrdDisplayName,
                HrdShowButtonWithDomain = upParty.HrdShowButtonWithDomain,
                HrdDomains = upParty.HrdDomains,
                HrdLogoUrl = upParty.HrdLogoUrl,
                DisableUserAuthenticationTrust = upParty.DisableUserAuthenticationTrust,
                DisableTokenExchangeTrust = upParty.DisableTokenExchangeTrust,
                Remove = remove
            };
            await message.ValidateObjectAsync();

            var routeBinding = RouteBinding;
            backgroundQueue.QueueBackgroundWorkItem(async (stoppingToken) =>
            {
                try
                {
                    var info = $"{(remove ? "Remove" : "Update")} authentication method '{upParty.Name}' {(remove ? "from" : "in")} application registrations allow authentication method list";                                       
                    logger.Event($"Start to process '{info}'.");
                    await DoWorkAsync(routeBinding.TenantName, routeBinding.TrackName, message, stoppingToken);
                    logger.Event($"Done processing '{info}'.");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Background queue error.");
                }
            });
        }

        public async Task DoWorkAsync(string tenantName, string trackName, UpPartyHrdQueueMessage message, CancellationToken stoppingToken)
        {
            var idKey = new Track.IdKey { TenantName = tenantName, TrackName = trackName };
            string paginationToken = null;
            while (!stoppingToken.IsCancellationRequested) 
            {
                (var downParties, paginationToken) = await tenantDataRepository.GetListAsync<DownParty>(idKey, whereQuery: p => p.DataType == Constants.Models.DataType.DownParty && p.AllowUpParties.Where(up => up.Name == message.Name).Any(), pageSize: 100, paginationToken: paginationToken, scopedLogger: logger);
                stoppingToken.ThrowIfCancellationRequested();
                foreach (var downParty in downParties)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    await UpdateDownPartyAsync(tenantName, trackName, downParty, message);
                }
                
                if (paginationToken == null)
                {
                    break;
                } 
            }
        }

        private async Task UpdateDownPartyAsync(string tenantName, string trackName, DownParty downParty, UpPartyHrdQueueMessage message)
        {
            switch (downParty.Type)
            {
                case PartyTypes.Oidc:
                    await UpdateDownPartyAsync<OidcDownParty>(tenantName, trackName, downParty, message);
                    break;
                case PartyTypes.Saml2:
                    await UpdateDownPartyAsync<SamlDownParty>(tenantName, trackName, downParty, message);
                    break;
                case PartyTypes.OAuth2:
                    await UpdateDownPartyAsync<OAuthDownParty>(tenantName, trackName, downParty, message);
                    break;
                default:
                    throw new NotSupportedException($"Application registration type {downParty.Type} not supported.");
            }
        }

        private async Task UpdateDownPartyAsync<T>(string tenantName, string trackName, DownParty downParty, UpPartyHrdQueueMessage messageObj) where T : DownParty
        {
            var idKey = new Party.IdKey
            {
                TenantName = tenantName,
                TrackName = trackName,
                PartyName = downParty.Name
            };
            var downPartyFullObj = await tenantDataRepository.GetAsync<T>(await DownParty.IdFormatAsync(idKey), scopedLogger: logger);
            var upParty = downPartyFullObj.AllowUpParties.Where(up => up.Name == messageObj.Name).FirstOrDefault();
            if (upParty != null)
            {
                if (!messageObj.Remove)
                {
                    upParty.DisplayName = messageObj.DisplayName;
                    upParty.Issuers = messageObj.Issuers;
                    upParty.SpIssuer = messageObj.SpIssuer;
                    upParty.HrdDomains = messageObj.HrdDomains;
                    upParty.HrdShowButtonWithDomain = messageObj.HrdShowButtonWithDomain;
                    upParty.HrdDisplayName = messageObj.HrdDisplayName;
                    upParty.HrdLogoUrl = messageObj.HrdLogoUrl;
                    upParty.DisableUserAuthenticationTrust = messageObj.DisableUserAuthenticationTrust;
                    upParty.DisableTokenExchangeTrust = messageObj.DisableTokenExchangeTrust;
                }
                else
                {
                    downPartyFullObj.AllowUpParties.Remove(upParty);
                }

                await tenantDataRepository.UpdateAsync(downPartyFullObj, scopedLogger: logger);
                await downPartyCacheLogic.InvalidateDownPartyCacheAsync(idKey);
            }
        }
    }
}
