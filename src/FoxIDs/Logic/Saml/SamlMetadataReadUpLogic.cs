using FoxIDs.Infrastructure;
using FoxIDs.Logic.Caches.Providers;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SamlMetadataReadUpLogic : LogicSequenceBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly ICacheProvider cacheProvider;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SamlMetadataReadLogic samlMetadataReadLogic;

        public SamlMetadataReadUpLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, ICacheProvider cacheProvider, ITenantDataRepository tenantDataRepository, SamlMetadataReadLogic samlMetadataReadLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.cacheProvider = cacheProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.samlMetadataReadLogic = samlMetadataReadLogic;
        }

        public async Task<SamlUpParty> CheckMetadataAndUpdateUpPartyAsync(SamlUpParty party)
        {
            if (party.UpdateState != PartyUpdateStates.Automatic)
            {
                return party;
            }

            var lastUpdated = DateTimeOffset.FromUnixTimeSeconds(party.LastUpdated);
            if (lastUpdated.AddSeconds(party.MetadataUpdateRate.Value) >= DateTimeOffset.UtcNow)
            {
                return party;
            }

            var key = UpdateUpPartyWaitPeriodKey(party.Id);
            if (await cacheProvider.ExistsAsync(key))
            {
                logger.ScopeTrace(() => $"Authentication method '{party.Id}' not updated with SAML 2.0 metadata because another update is in progress.");
                return party;
            }
            else
            {
                await cacheProvider.SetFlagAsync(key, settings.UpPartyUpdateWaitPeriod);
            }

            var failingUpdateCount = await cacheProvider.GetNumberAsync(FailingUpdateUpPartyCountKey(party.Id));
            if (failingUpdateCount >= settings.UpPartyMaxFailingUpdate)
            {
                party.UpdateState = PartyUpdateStates.AutomaticStopped;
                await tenantDataRepository.SaveAsync(party);
                await cacheProvider.DeleteAsync(FailingUpdateUpPartyCountKey(party.Id));
                return party;
            }

            try
            {
                try
                {
                    party = await samlMetadataReadLogic.PopulateModelAsync(party);
                }
                catch (Exception ex)
                {
                    throw new EndpointException("Failed to read SAML 2.0 metadata.", ex) { RouteBinding = RouteBinding };
                }

                await tenantDataRepository.SaveAsync(party);
                logger.ScopeTrace(() => $"Authentication method '{party.Id}' updated by SAML 2.0 metadata.", triggerEvent: true);

                await cacheProvider.DeleteAsync(FailingUpdateUpPartyCountKey(party.Id));
            }
            catch (Exception ex)
            {
                await cacheProvider.IncrementNumberAsync(FailingUpdateUpPartyCountKey(party.Id));
                logger.Warning(ex);
            }

            return party;
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
