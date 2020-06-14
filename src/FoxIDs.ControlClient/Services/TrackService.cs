using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class TrackService : BaseService
    {
        private const string apiUri = "api/{tenant}/master/!track";
        private const string filterApiUri = "api/{tenant}/master/!filtertrack";
        private readonly HttpClient httpClient;

        public TrackService(HttpClient httpClient, RouteBindingLogic routeBindingLogic) : base(routeBindingLogic)
        {
            this.httpClient = httpClient;
        }

        public async Task<IEnumerable<Track>> SearchTrackAsync(string filterName, string tenantName = null)
        {
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(filterApiUri, tenantName)}?filterName={filterName}");
            var tracks = await response.ToObjectAsync<IEnumerable<Track>>();
            return tracks;
        }

        public async Task CreateTrackAsync(Track track, string tenantName = null)
        {
            using var response = await httpClient.PostAsJsonAsync(await GetTenantApiUrlAsync(apiUri, tenantName), track);
            var trackResponse = await response.ToObjectAsync<Track>();
        }
    }
}
