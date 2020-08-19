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
        private const string filterApiUri = "api/{tenant}/master/!filtertrack";

        public TrackService(HttpClient httpClient, RouteBindingLogic routeBindingLogic) : base(httpClient, routeBindingLogic)
        { }

        public async Task<IEnumerable<Track>> FilterTrackAsync(string filterName, string tenantName = null) => await FilterAsync<Track>(filterApiUri, filterName, tenantName: tenantName);

        public async Task CreateTrackAsync(Track track, string tenantName = null) => await CreateAsync(apiUri, track, tenantName: tenantName);
    }
}
