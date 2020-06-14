using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class UpPartyService : BaseService
    {
        private const string apiUri = "api/{tenant}/master/!upparty";
        private const string filterApiUri = "api/{tenant}/master/!filterupparty";
        private readonly HttpClient httpClient;

        public UpPartyService(HttpClient httpClient, RouteBindingLogic routeBindingLogic) : base(routeBindingLogic)
        {
            this.httpClient = httpClient;
        }

        public async Task<IEnumerable<UpParty>> FilterUpPartyAsync(string filterName)
        {
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(filterApiUri)}?filterName={filterName}");
            var upParties = await response.ToObjectAsync<IEnumerable<UpParty>>();
            return upParties;

        }
    }
}
