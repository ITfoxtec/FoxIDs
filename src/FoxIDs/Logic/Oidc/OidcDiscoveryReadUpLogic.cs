using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OidcDiscoveryReadUpLogic<TParty, TClient> : LogicSequenceBase where TParty : OAuthUpParty<TClient> where TClient : OAuthUpClient
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IDistributedCacheProvider cacheProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly OidcDiscoveryReadLogic<TParty, TClient> oidcDiscoveryReadLogic;

        public OidcDiscoveryReadUpLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, IDistributedCacheProvider cacheProvider, ITenantRepository tenantRepository, OidcDiscoveryReadLogic<TParty, TClient> oidcDiscoveryReadLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.cacheProvider = cacheProvider;
            this.tenantRepository = tenantRepository;
            this.oidcDiscoveryReadLogic = oidcDiscoveryReadLogic;
        }

        public async Task CheckOidcDiscoveryAndUpdatePartyAsync(TParty party)
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

            var key = UpdateWaitPeriodKey(party.Id);
            if (await cacheProvider.ExistsAsync(key))
            {
                logger.ScopeTrace(() => $"Authentication method '{party.Id}' not updated with OIDC discovery because another update is in progress.");
                return;
            }
            else
            {
                await cacheProvider.SetAsync(key, "true", settings.UpPartyUpdateWaitPeriod);
            }

            var failingUpdateCountString = await cacheProvider.GetAsync(FailingUpdateCountKey(party.Id));
            var failingUpdateCount = failingUpdateCountString != null ? long.Parse(failingUpdateCountString) : 0;
            if (failingUpdateCount >= settings.UpPartyMaxFailingUpdate)
            {
                party.UpdateState = PartyUpdateStates.AutomaticStopped;
                await tenantRepository.SaveAsync(party);
                await cacheProvider.DeleteAsync(FailingUpdateCountKey(party.Id));
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
                logger.ScopeTrace(() => $"Authentication method '{party.Id}' updated by OIDC discovery.", triggerEvent: true);

                await cacheProvider.DeleteAsync(FailingUpdateCountKey(party.Id));
            }
            catch (Exception ex)
            {
                var failingCountString = await cacheProvider.GetAsync(FailingUpdateCountKey(party.Id));
                var failingCount = failingCountString != null ? long.Parse(failingCountString) : 0;
                failingCount++;
                await cacheProvider.SetAsync(FailingUpdateCountKey(party.Id), failingCount.ToString(), settings.UpPartyMaxFailingUpdate);
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
