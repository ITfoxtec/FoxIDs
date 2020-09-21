using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class TrackService : BaseService
    {
        private const string apiUri = "api/{tenant}/master/!track";
        private const string filterApiUri = "api/{tenant}/master/!filtertrack";

        private const string keyContainedApiUri = "api/{tenant}/{track}/!trackkeycontained";
        private const string keyContainedSwapApiUri = "api/{tenant}/{track}/!trackkeyscontainedwap";
        private const string keyTypeApiUri = "api/{tenant}/{track}/!trackkeytype";
        private const string filterResourceNameApiUri = "api/{tenant}/master/!filterresourcename";
        private const string resourceApiUri = "api/{tenant}/{track}/!trackresource";

        public TrackService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<IEnumerable<Track>> FilterTrackAsync(string filterName) => await FilterAsync<Track>(filterApiUri, filterName);

        public async Task<Track> GetTrackAsync(string name) => await GetAsync<Track>(apiUri, name);
        public async Task CreateTrackAsync(Track track) => await PostAsync(apiUri, track);
        public async Task UpdateTrackAsync(Track track) => await PutAsync(apiUri, track);
        public async Task DeleteTrackAsync(string name) => await DeleteAsync(apiUri, name);

        public async Task<TrackKeyItemsContained> GetTrackKeyContainedAsync() => await GetAsync<TrackKeyItemsContained>(keyContainedApiUri);
        public async Task<TrackKeyItemsContained> UpdateTrackKeyContainedAsync(TrackKeyItemContainedRequest trackKeyRequest) => await PutResponseAsync<TrackKeyItemContainedRequest, TrackKeyItemsContained>(keyContainedApiUri, trackKeyRequest);
        public async Task DeleteTrackKeyContainedAsync() => await DeleteAsync(keyContainedApiUri);

        public async Task SwapTrackKeyContainedAsync(TrackKeyItemContainedSwap trackKeySwap) => await PostAsync(keyContainedSwapApiUri, trackKeySwap);

        public async Task<TrackKey> GetTrackKeyTypeAsync() => await GetAsync<TrackKey>(keyTypeApiUri);
        public async Task UpdateTrackKeyTypeAsync(TrackKey trackKeyRequest) => await PutAsync<TrackKey>(keyTypeApiUri, trackKeyRequest);

        public async Task<IEnumerable<ResourceName>> FilterResourceNameAsync(string filterName) => await FilterAsync<ResourceName>(filterResourceNameApiUri, filterName);

        public async Task<ResourceItem> GetTrackResourceAsync(int resourceId) => await GetAsync<ResourceItem>(resourceApiUri, Convert.ToString(resourceId), parmName: nameof(resourceId));
        public async Task UpdateTrackResourceAsync(TrackResourceItem trackResourceItem) => await PutAsync(resourceApiUri, trackResourceItem);
        public async Task DeleteTrackResourceAsync(int resourceId) => await DeleteAsync(resourceApiUri, Convert.ToString(resourceId), parmName: nameof(resourceId));

    }
}
