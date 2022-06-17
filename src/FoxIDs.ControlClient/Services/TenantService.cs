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

        public TenantService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<IEnumerable<Tenant>> FilterTenantAsync(string filterValue) => await FilterAsync<Tenant>(filterApiUri, filterValue, parmValue2: filterValue, parmName2: "filterCustomDomain");

        public async Task<Tenant> GetTenantAsync(string name) => await GetAsync<Tenant>(apiUri, name);
        public async Task CreateTenantAsync(CreateTenantRequest tenant) => await PostAsync(apiUri, tenant);
        public async Task<Tenant> UpdateTenantAsync(TenantRequest tenant) => await PutResponseAsync<TenantRequest, Tenant>(apiUri, tenant);
        public async Task DeleteTenantAsync(string name) => await DeleteAsync(apiUri, name);
    }
}
