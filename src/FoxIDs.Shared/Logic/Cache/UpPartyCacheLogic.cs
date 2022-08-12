using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class UpPartyCacheLogic : LogicBase
    {
        private readonly Settings settings;
        private readonly IConnectionMultiplexer redisConnectionMultiplexer;
        private readonly ITenantRepository tenantRepository;

        public UpPartyCacheLogic(Settings settings, IConnectionMultiplexer redisConnectionMultiplexer, ITenantRepository tenantRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.redisConnectionMultiplexer = redisConnectionMultiplexer;
            this.tenantRepository = tenantRepository;
        }

        public async Task InvalidateUpPartyCacheAsync(string upPartyName, string tenantName = null, string trackName = null)
        {
            var key = RadisUpPartyNameKey(GetUpPartyIdKey(upPartyName, tenantName, trackName));
            var db = redisConnectionMultiplexer.GetDatabase();
            await db.KeyDeleteAsync(key);
        }

        public async Task<UpParty> GetUpPartyAsync(string upPartyName, string tenantName = null, string trackName = null, bool required = true)
        {
            var key = RadisUpPartyNameKey(GetUpPartyIdKey(upPartyName, tenantName, trackName));
            var db = redisConnectionMultiplexer.GetDatabase();

            var upPartyAsString = (string)await db.StringGetAsync(key);
            if (!upPartyAsString.IsNullOrEmpty())
            {
                return upPartyAsString.ToObject<UpParty>();
            }

            var upParty = await tenantRepository.GetAsync<UpParty>(await UpParty.IdFormatAsync(GetUpPartyIdKey(upPartyName, tenantName, trackName)), required: required);
            if (upParty != null)
            {
                await db.StringSetAsync(key, upParty.ToJson(), TimeSpan.FromSeconds(settings.Cache.UpPartyLifetime));
            }
            return upParty;
        }

        private string RadisUpPartyNameKey(Party.IdKey partyIdKey)
        {          
            return $"upParty_name_{partyIdKey.TenantName}_{partyIdKey.TrackName}_{partyIdKey.PartyName}";
        }

        private Party.IdKey GetUpPartyIdKey(string upPartyName, string tenantName = null, string trackName = null)
        {
            if (tenantName.IsNullOrEmpty() || trackName.IsNullOrEmpty())
            {
                var routeBinding = RouteBinding;
                if (routeBinding == null)
                {
                    throw new InvalidOperationException("RouteBinding is null in up-party cache.");
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
