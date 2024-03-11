using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class UpPartyCacheLogic : LogicBase
    {
        private readonly Settings settings;
        private readonly IDistributedCacheProvider cacheProvider;
        private readonly ITenantRepository tenantRepository;

        public UpPartyCacheLogic(Settings settings, IDistributedCacheProvider cacheProvider, ITenantRepository tenantRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.cacheProvider = cacheProvider;
            this.tenantRepository = tenantRepository;
        }

        public async Task InvalidateUpPartyCacheAsync(Party.IdKey idKey)
        {
            var key = RadisUpPartyNameKey(idKey);
            await cacheProvider.DeleteAsync(key);
        }

        public async Task InvalidateUpPartyCacheAsync(string upPartyName, string tenantName = null, string trackName = null)
        {
            await InvalidateUpPartyCacheAsync(GetUpPartyIdKey(upPartyName, tenantName, trackName));
        }

        public async Task<UpParty> GetUpPartyAsync(Party.IdKey idKey, bool required = true)
        {
            var key = RadisUpPartyNameKey(idKey);

            var upPartyAsString = await cacheProvider.GetAsync(key);
            if (!upPartyAsString.IsNullOrEmpty())
            {
                return upPartyAsString.ToObject<UpParty>();
            }

            var upParty = await tenantRepository.GetAsync<UpParty>(await UpParty.IdFormatAsync(idKey), required: required);
            if (upParty != null)
            {
                await cacheProvider.SetAsync(key, upParty.ToJson(), settings.Cache.UpPartyLifetime);
            }
            return upParty;
        }

        public async Task<UpParty> GetUpPartyAsync(string upPartyName, string tenantName = null, string trackName = null, bool required = true)
        {
            return await GetUpPartyAsync(GetUpPartyIdKey(upPartyName, tenantName, trackName), required);
        }

        private string RadisUpPartyNameKey(Party.IdKey partyIdKey)
        {          
            return $"upParty_cache_name_{partyIdKey.TenantName}_{partyIdKey.TrackName}_{partyIdKey.PartyName}";
        }

        private Party.IdKey GetUpPartyIdKey(string upPartyName, string tenantName = null, string trackName = null)
        {
            if (tenantName.IsNullOrEmpty() || trackName.IsNullOrEmpty())
            {
                var routeBinding = RouteBinding;
                if (routeBinding == null)
                {
                    throw new InvalidOperationException("RouteBinding is null in authentication method cache.");
                }
                tenantName = routeBinding.TenantName;
                trackName = routeBinding.TrackName;
            }

            return new Party.IdKey
            {
                TenantName = tenantName,
                TrackName = trackName,
                PartyName = upPartyName
            };
        }
    }
}
