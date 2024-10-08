﻿using FoxIDs.Infrastructure;
using FoxIDs.Logic.Caches.Providers;
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
        private readonly ICacheProvider cacheProvider;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly OidcDiscoveryReadModelLogic<TParty, TClient> oidcDiscoveryReadModelLogic;

        public OidcDiscoveryReadUpLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, ICacheProvider cacheProvider, ITenantDataRepository tenantDataRepository, OidcDiscoveryReadModelLogic<TParty, TClient> oidcDiscoveryReadModelLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.cacheProvider = cacheProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.oidcDiscoveryReadModelLogic = oidcDiscoveryReadModelLogic;
        }

        public async Task<TParty> CheckOidcDiscoveryAndUpdatePartyAsync(TParty party)
        {
            if (party.UpdateState != PartyUpdateStates.Automatic)
            {
                return party;
            }

            var lastUpdated = DateTimeOffset.FromUnixTimeSeconds(party.LastUpdated);
            if (lastUpdated.AddSeconds(party.OidcDiscoveryUpdateRate.Value) >= DateTimeOffset.UtcNow)
            {
                return party;
            }

            var key = UpdateWaitPeriodKey(party.Id);
            if (await cacheProvider.ExistsAsync(key))
            {
                logger.ScopeTrace(() => $"Authentication method '{party.Id}' not updated with OIDC discovery because another update is in progress.");
                return party;
            }
            else
            {
                await cacheProvider.SetFlagAsync(key, settings.UpPartyUpdateWaitPeriod);
            }

            var failingUpdateCount = await cacheProvider.GetNumberAsync(FailingUpdateCountKey(party.Id));
            if (failingUpdateCount >= settings.UpPartyMaxFailingUpdate)
            {
                party.UpdateState = PartyUpdateStates.AutomaticStopped;
                await tenantDataRepository.SaveAsync(party);
                await cacheProvider.DeleteAsync(FailingUpdateCountKey(party.Id));
                return party;
            }

            try
            {
                try
                {
                    party = await oidcDiscoveryReadModelLogic.PopulateModelAsync(party);
                }
                catch (Exception ex)
                {
                    throw new EndpointException("Failed to read OIDC discovery.", ex) { RouteBinding = RouteBinding };
                }

                await tenantDataRepository.SaveAsync(party);
                logger.ScopeTrace(() => $"Authentication method '{party.Id}' updated by OIDC discovery.", triggerEvent: true);

                await cacheProvider.DeleteAsync(FailingUpdateCountKey(party.Id));
            }
            catch (Exception ex)
            {
                await cacheProvider.IncrementNumberAsync(FailingUpdateCountKey(party.Id));
                logger.Warning(ex);
            }

            return party;
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
