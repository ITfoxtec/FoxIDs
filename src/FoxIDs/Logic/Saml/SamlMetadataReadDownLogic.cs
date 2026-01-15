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
    public class SamlMetadataReadDownLogic : LogicSequenceBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly ICacheProvider cacheProvider;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SamlMetadataReadLogic samlMetadataReadLogic;

        public SamlMetadataReadDownLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, ICacheProvider cacheProvider, ITenantDataRepository tenantDataRepository, SamlMetadataReadLogic samlMetadataReadLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.cacheProvider = cacheProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.samlMetadataReadLogic = samlMetadataReadLogic;
        }

        public async Task<SamlDownParty> CheckMetadataAndUpdateDownPartyAsync(SamlDownParty party)
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

            var key = UpdateDownPartyWaitPeriodKey(party.Id);
            if (await cacheProvider.ExistsAsync(key))
            {
                logger.ScopeTrace(() => $"Application registration '{party.Id}' not updated with SAML 2.0 metadata because another update is in progress.");
                return party;
            }
            else
            {
                await cacheProvider.SetFlagAsync(key, settings.UpPartyUpdateWaitPeriod);
            }

            var failingUpdateCount = await cacheProvider.GetNumberAsync(FailingUpdateDownPartyCountKey(party.Id));
            if (failingUpdateCount >= settings.UpPartyMaxFailingUpdate)
            {
                party.UpdateState = PartyUpdateStates.AutomaticStopped;
                await tenantDataRepository.SaveAsync(party);
                await cacheProvider.DeleteAsync(FailingUpdateDownPartyCountKey(party.Id));
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
                logger.ScopeTrace(() => $"Application registration '{party.Id}' updated by SAML 2.0 metadata.", triggerEvent: true);

                await cacheProvider.DeleteAsync(FailingUpdateDownPartyCountKey(party.Id));
            }
            catch (Exception ex)
            {
                await cacheProvider.IncrementNumberAsync(FailingUpdateDownPartyCountKey(party.Id));
                logger.Warning(ex);
            }

            return party;
        }

        private string UpdateDownPartyWaitPeriodKey(string partyId)
        {
            return $"update_down_party_wait_period_{RouteBinding.TenantNameDotTrackName}_{partyId}";
        }

        private string FailingUpdateDownPartyCountKey(string partyId)
        {
            return $"failing_down_party_update_count_{RouteBinding.TenantNameDotTrackName}_{partyId}";
        }
    }
}
