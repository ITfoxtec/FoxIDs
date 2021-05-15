using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OidcDiscoveryReadUpLogic : LogicBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IConnectionMultiplexer redisConnectionMultiplexer;
        private readonly ITenantRepository tenantRepository;
        private readonly OidcDiscoveryReadLogic oidcDiscoveryReadLogic;

        public OidcDiscoveryReadUpLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, IConnectionMultiplexer redisConnectionMultiplexer, ITenantRepository tenantRepository, OidcDiscoveryReadLogic oidcDiscoveryReadLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.redisConnectionMultiplexer = redisConnectionMultiplexer;
            this.tenantRepository = tenantRepository;
            this.oidcDiscoveryReadLogic = oidcDiscoveryReadLogic;
        }

        public async Task CheckOidcDiscoveryAndUpdatePartyAsync(OidcUpParty party)
        {
            if (party.UpdateState != PartyUpdateStates.Automatic)
            {
                return;
            }

            var lastUpdated = DateTimeOffset.FromUnixTimeSeconds(party.LastUpdated);
            if (lastUpdated.AddSeconds(party.OidcDiscoveryUpdateRate.Value) >= DateTimeOffset.UtcNow)
            {
                return;
            }

            var db = redisConnectionMultiplexer.GetDatabase();
            var key = UpdateWaitPeriodKey(party.Id);
            if (await db.KeyExistsAsync(key))
            {
                logger.ScopeTrace(() => $"Up party '{party.Id}' not updated with OIDC discovery because another update is in progress.");
                return;
            }
            else
            {
                await db.StringSetAsync(key, true, TimeSpan.FromSeconds(settings.UpPartyUpdateWaitPeriod));
            }

            var failingUpdateCount = (long?)await db.StringGetAsync(FailingUpdateCountKey(party.Id));
            if (failingUpdateCount.HasValue && failingUpdateCount.Value >= settings.UpPartyMaxFailingUpdate)
            {
                party.UpdateState = PartyUpdateStates.AutomaticStopped;
                await tenantRepository.SaveAsync(party);
                await db.KeyDeleteAsync(FailingUpdateCountKey(party.Id));
                return;
            }

            try
            {
                try
                {
                    await oidcDiscoveryReadLogic.PopulateModelAsync(party);
                }
                catch (Exception ex)
                {
                    throw new EndpointException("Failed to read OIDC discovery.", ex) { RouteBinding = RouteBinding };
                }

                await tenantRepository.SaveAsync(party);
                logger.ScopeTrace(() => $"Up party '{party.Id}' updated by OIDC discovery.", triggerEvent: true);

                await db.KeyDeleteAsync(FailingUpdateCountKey(party.Id));
            }
            catch (Exception ex)
            {
                await db.StringIncrementAsync(FailingUpdateCountKey(party.Id));
                logger.Warning(ex);
            }
        }

        private string UpdateWaitPeriodKey(string partyId)
        {
            return $"update_up_party_wait_period_{RouteBinding.TenantNameDotTrackName}_{partyId}";
        }

        private string FailingUpdateCountKey(string partyId)
        {
            return $"failing_up_party_update_count_{RouteBinding.TenantNameDotTrackName}_{partyId}";
        }
    }
}
