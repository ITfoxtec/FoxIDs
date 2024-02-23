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
    public class TrackCacheLogic : LogicBase
    {
        private readonly Settings settings;
        private readonly IConnectionMultiplexer redisConnectionMultiplexer;
        private readonly ITenantRepository tenantRepository;

        public TrackCacheLogic(Settings settings, IConnectionMultiplexer redisConnectionMultiplexer, ITenantRepository tenantRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.redisConnectionMultiplexer = redisConnectionMultiplexer;
            this.tenantRepository = tenantRepository;
        }

        public async Task InvalidateTrackCacheAsync(Track.IdKey idKey)
        {
            var key = RadisTrackNameKey(idKey);
            var db = redisConnectionMultiplexer.GetDatabase();
            await db.KeyDeleteAsync(key);
        }

        public async Task InvalidateTrackCacheAsync(string trackName, string tenantName = null)
        {
            await InvalidateTrackCacheAsync(GetTrackIdKey(trackName, tenantName));
        }

        public async Task<Track> GetTrackAsync(Track.IdKey idKey, bool required = true)
        {
            var key = RadisTrackNameKey(idKey);
            var db = redisConnectionMultiplexer.GetDatabase();

            var trackAsString = (string)await db.StringGetAsync(key);
            if (!trackAsString.IsNullOrEmpty())
            {
                return trackAsString.ToObject<Track>();
            }

            var track = await tenantRepository.GetAsync<Track>(await Track.IdFormatAsync(idKey), required: required);
            if (track != null)
            {
                await db.StringSetAsync(key, track.ToJson(), TimeSpan.FromSeconds(settings.Cache.TrackLifetime));
            }
            return track;
        }

        public async Task<Track> GetTrackAsync(string trackName, string tenantName = null, bool required = true)
        {
            return await GetTrackAsync(GetTrackIdKey(trackName, tenantName), required);
        }

        private string RadisTrackNameKey(Track.IdKey trackIdKey)
        {
            return $"track_cache_name_{trackIdKey.TenantName}_{trackIdKey.TrackName}";
        }

        private Track.IdKey GetTrackIdKey(string trackName, string tenantName = null)
        {
            if (tenantName.IsNullOrEmpty() || trackName.IsNullOrEmpty())
            {
                var routeBinding = RouteBinding;
                if (routeBinding == null)
                {
                    throw new InvalidOperationException("RouteBinding is null in Environment cache.");
                }
                tenantName = routeBinding.TenantName;
            }

            return new Track.IdKey
            {
                TenantName = tenantName,
                TrackName = trackName
            };
        }
    }
}
