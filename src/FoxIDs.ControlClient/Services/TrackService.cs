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
        private const string resourceApiUri = "api/{tenant}/master/!trackresource";

        public TrackService(HttpClient httpClient, RouteBindingLogic routeBindingLogic) : base(httpClient, routeBindingLogic)
        { }

        public async Task<IEnumerable<Track>> FilterTrackAsync(string filterName, string tenantName = null) => await FilterAsync<Track>(filterApiUri, filterName, tenantName: tenantName);

        public async Task CreateTrackAsync(Track track, string tenantName = null) => await PostAsync(apiUri, track, tenantName: tenantName);

        public async Task<TrackKeys> GetTrackKeyAsync(string trackName) => await GetAsync<TrackKeys>(keyApiUri, trackName, parmName: nameof(trackName));
        public async Task UpdateTrackKeyAsync(TrackKeyRequest trackKeyRequest) => await PutAsync(keyApiUri, trackKeyRequest);
        public async Task DeleteTrackKeyAsync(string trackName) => await DeleteAsync(keyApiUri, trackName, parmName: nameof(trackName));

        public async Task SwapTrackKeyAsync(TrackKeySwap trackKeySwap) => await PostAsync(keySwapApiUri, trackKeySwap);

        public async Task<IEnumerable<ResourceName>> FilterResourceNameAsync(string filterName, string tenantName = null) => await FilterAsync<ResourceName>(filterResourceNameApiUri, filterName, tenantName: tenantName);

        public async Task<ResourceItem> GetTrackResourceAsync(string trackName, int resourceId) => await GetAsync<ResourceItem>(resourceApiUri, trackName, Convert.ToString(resourceId), parmName1: nameof(trackName), parmName2: nameof(resourceId));
        public async Task UpdateTrackResourceAsync(TrackResourceItem trackResourceItem) => await PutAsync(resourceApiUri, trackResourceItem);
        public async Task DeleteTrackResourceAsync(string trackName, int resourceId) => await DeleteAsync(resourceApiUri, trackName, Convert.ToString(resourceId), parmName1: nameof(trackName), parmName2: nameof(resourceId));

    }
}
