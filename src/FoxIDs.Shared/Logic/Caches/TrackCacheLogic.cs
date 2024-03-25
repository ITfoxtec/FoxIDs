using FoxIDs.Logic.Caches.Providers;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class TrackCacheLogic : LogicBase
    {
        private readonly Settings settings;
        private readonly IDataCacheProvider cacheProvider;
        private readonly ITenantDataRepository tenantDataRepository;

        public TrackCacheLogic(Settings settings, IDataCacheProvider cacheProvider, ITenantDataRepository tenantDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.cacheProvider = cacheProvider;
            this.tenantDataRepository = tenantDataRepository;
        }

        public async Task InvalidateTrackCacheAsync(Track.IdKey idKey)
        {
            var key = RadisTrackNameKey(idKey);
            await cacheProvider.DeleteAsync(key);
        }

        public async Task InvalidateTrackCacheAsync(string trackName, string tenantName = null)
        {
            await InvalidateTrackCacheAsync(GetTrackIdKey(trackName, tenantName));
        }

        public async Task<Track> GetTrackAsync(Track.IdKey idKey, bool required = true)
        {
            var key = RadisTrackNameKey(idKey);

            var trackAsString = await cacheProvider.GetAsync(key);
            if (!trackAsString.IsNullOrEmpty())
            {
                return trackAsString.ToObject<Track>();
            }

            var track = await tenantDataRepository.GetAsync<Track>(await Track.IdFormatAsync(idKey), required: required);
            if (track != null)
            {
                await cacheProvider.SetAsync(key, track.ToJson(), settings.Cache.TrackLifetime);
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
