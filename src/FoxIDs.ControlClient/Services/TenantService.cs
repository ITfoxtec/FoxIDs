using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class TenantService : BaseService
    {
        private const string apiUri = "api/{tenant}/master/!tenant";
        private const string filterApiUri = "api/{tenant}/master/!filtertenant";

        public TenantService(HttpClient httpClient, RouteBindingLogic routeBindingLogic) : base(httpClient, routeBindingLogic)
        { }

        public async Task<IEnumerable<Tenant>> FilterTenantAsync(string filterName) => await FilterAsync<Tenant>(filterApiUri, filterName);

        public async Task CreateTenantAsync(CreateTenantRequest tenant) => await CreateAsync(apiUri, tenant);
        public async Task DeleteTenantAsync(string name) => await DeleteAsync(apiUri, name);
    }
}
