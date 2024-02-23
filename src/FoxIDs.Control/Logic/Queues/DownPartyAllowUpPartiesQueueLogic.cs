using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Queue;
using FoxIDs.Models;
using FoxIDs.Models.Queue;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class DownPartyAllowUpPartiesQueueLogic : LogicBase, IQueueProcessingService
    {
        private const string downPartyDataType = "party:down";
        private readonly IConnectionMultiplexer redisConnectionMultiplexer;
        private readonly ITenantRepository tenantRepository;
        private readonly DownPartyCacheLogic downPartyCacheLogic;

        public DownPartyAllowUpPartiesQueueLogic(IConnectionMultiplexer redisConnectionMultiplexer, ITenantRepository tenantRepository, DownPartyCacheLogic downPartyCacheLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.redisConnectionMultiplexer = redisConnectionMultiplexer;
            this.tenantRepository = tenantRepository;
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
            var envalope = new QueueEnvelope
            {
                TenantName = routeBinding.TenantName,
                TrackName = routeBinding.TrackName,
                Logging = routeBinding.Logging,
                LogicClassTypeFullName = GetType().FullName,
                Info = remove ? $"Remove authentication method '{upParty.Name}' from down-parties allow authentication method list" : $"Update authentication method '{upParty.Name}' in down-parties allow authentication method list",
                Message = message.ToJson(),
            };
            if (routeBinding.TelemetryClient != null)
            {
                envalope.ApplicationInsightsConnectionString = routeBinding.TelemetryClient.TelemetryConfiguration.ConnectionString;
            }
            await envalope.ValidateObjectAsync();

            var db = redisConnectionMultiplexer.GetDatabase();
            await db.ListLeftPushAsync(BackgroundQueueService.QueueKey, envalope.ToJson());

            var sub = redisConnectionMultiplexer.GetSubscriber();
            await sub.PublishAsync(BackgroundQueueService.QueueEventKey, string.Empty);
        }

        public async Task DoWorkAsync(TelemetryScopedLogger scopedLogger, string tenantName, string trackName, string message, CancellationToken stoppingToken)
        {
            var messageObj = message.ToObject<UpPartyHrdQueueMessage>();
            await messageObj.ValidateObjectAsync();

            var idKey = new Track.IdKey { TenantName = tenantName, TrackName = trackName };
            string continuationToken = null;
            while (!stoppingToken.IsCancellationRequested) 
            {
                (var downParties, continuationToken) = await tenantRepository.GetListAsync<DownParty>(idKey, whereQuery: p => p.DataType == downPartyDataType && p.AllowUpParties.Where(up => up.Name == messageObj.Name).Any(), maxItemCount: 30, continuationToken: continuationToken, scopedLogger: scopedLogger);
                stoppingToken.ThrowIfCancellationRequested();
                foreach (var downParty in downParties)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    await UpdateDownPartyAsync(scopedLogger, tenantName, trackName, downParty, messageObj);
                }
                
                if (continuationToken == null)
                {
                    break;
                } 
            }
        }

        private async Task UpdateDownPartyAsync(TelemetryScopedLogger scopedLogger, string tenantName, string trackName, DownParty downParty, UpPartyHrdQueueMessage messageObj)
        {
            switch (downParty.Type)
            {
                case PartyTypes.Oidc:
                    await UpdateDownPartyAsync<OidcDownParty>(scopedLogger, tenantName, trackName, downParty, messageObj);
                    break;
                case PartyTypes.Saml2:
                    await UpdateDownPartyAsync<SamlDownParty>(scopedLogger, tenantName, trackName, downParty, messageObj);
                    break;
                case PartyTypes.OAuth2:
                    await UpdateDownPartyAsync<OAuthDownParty>(scopedLogger, tenantName, trackName, downParty, messageObj);
                    break;
                default:
                    throw new NotSupportedException($"Down-party type {downParty.Type} not supported.");
            }
        }

        private async Task UpdateDownPartyAsync<T>(TelemetryScopedLogger scopedLogger, string tenantName, string trackName, DownParty downParty, UpPartyHrdQueueMessage messageObj) where T : DownParty
        {
            var idKey = new Party.IdKey
            {
                TenantName = tenantName,
                TrackName = trackName,
                PartyName = downParty.Name
            };
            var downPartyFullObj = await tenantRepository.GetAsync<T>(await DownParty.IdFormatAsync(idKey), scopedLogger: scopedLogger);
            var upParty = downPartyFullObj.AllowUpParties.Where(up => up.Name == messageObj.Name).FirstOrDefault();
            if (upParty != null)
            {
                if (!messageObj.Remove)
                {
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

                await tenantRepository.UpdateAsync(downPartyFullObj, scopedLogger: scopedLogger);
                await downPartyCacheLogic.InvalidateDownPartyCacheAsync(idKey);
            }
        }
    }
}
