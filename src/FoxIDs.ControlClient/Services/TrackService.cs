using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
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

        public TrackService(HttpClient httpClient, RouteBindingLogic routeBindingLogic) : base(httpClient, routeBindingLogic)
        { }

        public async Task<IEnumerable<Track>> FilterTrackAsync(string filterName, string tenantName = null) => await FilterAsync<Track>(filterApiUri, filterName, tenantName: tenantName);

        public async Task CreateTrackAsync(Track track, string tenantName = null) => await PostAsync(apiUri, track, tenantName: tenantName);

        public async Task<TrackKeys> GetTrackKeyAsync(string trackName) => await GetAsync<TrackKeys>(keyApiUri, trackName, parmName: nameof(trackName));
        public async Task UpdateTrackKeyAsync(TrackKeyRequest trackKeyRequest) => await PutAsync(keyApiUri, trackKeyRequest);
        public async Task DeleteTrackKeyAsync(string trackName) => await DeleteAsync(keyApiUri, trackName, parmName: nameof(trackName));

        public async Task SwapTrackKeyAsync(TrackKeySwap trackKeySwap) => await PostAsync(keySwapApiUri, trackKeySwap);
    }
}
