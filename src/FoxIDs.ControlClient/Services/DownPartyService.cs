using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class DownPartyService : BaseService
    {
        private const string apiUri = "api/{tenant}/master/!downparty";
        private const string filterApiUri = "api/{tenant}/master/!filterdownparty";
        private readonly HttpClient httpClient;

        public DownPartyService(HttpClient httpClient, RouteBindingLogic routeBindingLogic) : base(routeBindingLogic)
        {
            this.httpClient = httpClient;
        }

        public async Task<IEnumerable<DownParty>> FilterDownPartyAsync(string filterName)
        {
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(filterApiUri)}?filterName={filterName}");
            var downParties = await response.ToObjectAsync<IEnumerable<DownParty>>();
            return downParties;

        }
    }
}
