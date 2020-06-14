using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class TenantService : BaseService
    {
        private const string apiUri = "api/{tenant}/master/!tenant";
        private const string filterApiUri = "api/{tenant}/master/!filtertenant";
        private readonly HttpClient httpClient;

        public TenantService(HttpClient httpClient, RouteBindingLogic routeBindingLogic) : base(routeBindingLogic)
        {
            this.httpClient = httpClient;
        }

        public async Task<IEnumerable<Tenant>> SearchTenantAsync(string filterName, string tenantName = null)
        {
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(filterApiUri, tenantName)}?filterName={filterName}");
            var tenants = await response.ToObjectAsync<IEnumerable<Tenant>>();
            return tenants;

        }

        public async Task CreateTenantAsync(CreateTenantRequest tenant, string tenantName = null)
        {
            using var response = await httpClient.PostAsJsonAsync(await GetTenantApiUrlAsync(apiUri, tenantName), tenant);
            var tenantResponse = await response.ToObjectAsync<Tenant>();
        }
    }
}
