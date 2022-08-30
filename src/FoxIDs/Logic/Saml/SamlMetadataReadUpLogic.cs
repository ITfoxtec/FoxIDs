using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SamlMetadataReadUpLogic : LogicSequenceBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IConnectionMultiplexer redisConnectionMultiplexer;
        private readonly ITenantRepository tenantRepository;
        private readonly SamlMetadataReadLogic samlMetadataReadLogic;

        public SamlMetadataReadUpLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, IConnectionMultiplexer redisConnectionMultiplexer, ITenantRepository tenantRepository, SamlMetadataReadLogic samlMetadataReadLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.redisConnectionMultiplexer = redisConnectionMultiplexer;
            this.tenantRepository = tenantRepository;
            this.samlMetadataReadLogic = samlMetadataReadLogic;
        }

        public async Task CheckMetadataAndUpdateUpPartyAsync(SamlUpParty party)
        {
            if (party.UpdateState != PartyUpdateStates.Automatic)
            {
                return;
            }

            var lastUpdated = DateTimeOffset.FromUnixTimeSeconds(party.LastUpdated);
            if (lastUpdated.AddSeconds(party.MetadataUpdateRate.Value) >= DateTimeOffset.UtcNow)
            {
                return;
            }

            var db = redisConnectionMultiplexer.GetDatabase();
            var key = UpdateUpPartyWaitPeriodKey(party.Id);
            if (await db.KeyExistsAsync(key))
            {
                logger.ScopeTrace(() => $"Up party '{party.Id}' not updated with SAML 2.0 metadata because another update is in progress.");
                return;
            }
            else
            {
                await db.StringSetAsync(key, true, TimeSpan.FromSeconds(settings.UpPartyUpdateWaitPeriod));
            }

            var failingUpdateCount = (long?)await db.StringGetAsync(FailingUpdateUpPartyCountKey(party.Id));
            if (failingUpdateCount.HasValue && failingUpdateCount.Value >= settings.UpPartyMaxFailingUpdate)
            {
                party.UpdateState = PartyUpdateStates.AutomaticStopped;
                await tenantRepository.SaveAsync(party);
                await db.KeyDeleteAsync(FailingUpdateUpPartyCountKey(party.Id));
                return;
            }

            try
            {
                try
                {
                    await samlMetadataReadLogic.PopulateModelAsync(party);
                }
                catch (Exception ex)
                {
                    throw new EndpointException("Failed to read SAML 2.0 metadata.", ex) { RouteBinding = RouteBinding };
                }

                await tenantRepository.SaveAsync(party);
                logger.ScopeTrace(() => $"Up party '{party.Id}' updated by SAML 2.0 metadata.", triggerEvent: true);

                await db.KeyDeleteAsync(FailingUpdateUpPartyCountKey(party.Id));
            }
            catch (Exception ex)
            {
                await db.StringIncrementAsync(FailingUpdateUpPartyCountKey(party.Id));
                logger.Warning(ex);
            }
        }

        private string UpdateUpPartyWaitPeriodKey(string partyId)
        {
            return $"update_up_party_wait_period_{RouteBinding.TenantNameDotTrackName}_{partyId}";
        }

        private string FailingUpdateUpPartyCountKey(string partyId)
        {
            return $"failing_up_party_update_count_{RouteBinding.TenantNameDotTrackName}_{partyId}";
        }
    }
}
