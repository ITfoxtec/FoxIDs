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
    public class DownPartyCacheLogic : LogicBase
    {
        private readonly Settings settings;
        private readonly IConnectionMultiplexer redisConnectionMultiplexer;
        private readonly ITenantRepository tenantRepository;

        public DownPartyCacheLogic(Settings settings, IConnectionMultiplexer redisConnectionMultiplexer, ITenantRepository tenantRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.redisConnectionMultiplexer = redisConnectionMultiplexer;
            this.tenantRepository = tenantRepository;
        }

        public async Task InvalidateDownPartyCacheAsync(string downPartyName, string tenantName = null, string trackName = null)
        {
            var key = RadisDownPartyNameKey(GetDownPartyIdKey(downPartyName, tenantName, trackName));
            var db = redisConnectionMultiplexer.GetDatabase();
            await db.KeyDeleteAsync(key);
        }

        public async Task<DownParty> GetDownPartyAsync(string downPartyName, string tenantName = null, string trackName = null, bool required = true)
        {
            var key = RadisDownPartyNameKey(GetDownPartyIdKey(downPartyName, tenantName, trackName));
            var db = redisConnectionMultiplexer.GetDatabase();

            var downPartyAsString = (string)await db.StringGetAsync(key);
            if (!downPartyAsString.IsNullOrEmpty())
            {
                return downPartyAsString.ToObject<DownParty>();
            }

            var downParty = await tenantRepository.GetAsync<DownParty>(await DownParty.IdFormatAsync(GetDownPartyIdKey(downPartyName, tenantName, trackName)), required: required);
            if (downParty != null)
            {
                await db.StringSetAsync(key, downParty.ToJson(), TimeSpan.FromSeconds(settings.Cache.DownPartyLifetime));
            }
            return downParty;
        }

        private string RadisDownPartyNameKey(Party.IdKey partyIdKey)
        {
            return $"downParty_name_{partyIdKey.TenantName}_{partyIdKey.TrackName}_{partyIdKey.PartyName}";
        }

        private Party.IdKey GetDownPartyIdKey(string downPartyName, string tenantName = null, string trackName = null)
        {
            if (tenantName.IsNullOrEmpty() || trackName.IsNullOrEmpty())
            {
                var routeBinding = RouteBinding;
                if (routeBinding == null)
                {
                    throw new InvalidOperationException("RouteBinding is null in down-party cache.");
                }
                tenantName = routeBinding.TenantName;
                trackName = routeBinding.TrackName;
            }

            return new Party.IdKey
            {
                TenantName = tenantName,
                TrackName = trackName,
                PartyName = downPartyName
            };
        }
    }
}
