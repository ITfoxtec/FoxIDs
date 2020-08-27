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
        private const string keyApiUri = "api/{tenant}/master/!trackkey";
        private const string keySwapApiUri = "api/{tenant}/master/!trackkeyswap";
        private const string filterApiUri = "api/{tenant}/master/!filtertrack";
        private const string filterResourceNameApiUri = "api/{tenant}/master/!filterresourcename";
        private const string resourceApiUri = "api/{tenant}/{track}/!trackresource";

        public TrackService(HttpClient httpClient, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClient, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<IEnumerable<Track>> FilterTrackAsync(string filterName) => await FilterAsync<Track>(filterApiUri, filterName);

        public async Task<Track> GetTrackAsync(string name) => await GetAsync<Track>(apiUri, name);

        public async Task CreateTrackAsync(Track track) => await PostAsync(apiUri, track);

        public async Task<TrackKeys> GetTrackKeyAsync(string trackName) => await GetAsync<TrackKeys>(keyApiUri, trackName, parmName: nameof(trackName));
        public async Task UpdateTrackKeyAsync(TrackKeyRequest trackKeyRequest) => await PutAsync(keyApiUri, trackKeyRequest);
        public async Task DeleteTrackKeyAsync(string trackName) => await DeleteAsync(keyApiUri, trackName, parmName: nameof(trackName));

        public async Task SwapTrackKeyAsync(TrackKeySwap trackKeySwap) => await PostAsync(keySwapApiUri, trackKeySwap);

        public async Task<IEnumerable<ResourceName>> FilterResourceNameAsync(string filterName) => await FilterAsync<ResourceName>(filterResourceNameApiUri, filterName);

        public async Task<ResourceItem> GetTrackResourceAsync(int resourceId) => await GetAsync<ResourceItem>(resourceApiUri, Convert.ToString(resourceId), parmName: nameof(resourceId));
        public async Task UpdateTrackResourceAsync(TrackResourceItem trackResourceItem) => await PutAsync(resourceApiUri, trackResourceItem);
        public async Task DeleteTrackResourceAsync(int resourceId) => await DeleteAsync(resourceApiUri, Convert.ToString(resourceId), parmName: nameof(resourceId));

    }
}
