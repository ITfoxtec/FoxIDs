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
        private const string logUsageApiUri = "api/{tenant}/master/!tenantlogusage";

        public TenantService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<IEnumerable<Tenant>> FilterTenantAsync(string filterValue) => await FilterAsync<Tenant>(filterApiUri, filterValue, parmValue2: filterValue, parmName2: "filterCustomDomain");

        public async Task<TenantResponse> GetTenantAsync(string name) => await GetAsync<TenantResponse>(apiUri, name);
        public async Task CreateTenantAsync(CreateTenantRequest tenant) => await PostAsync(apiUri, tenant);
        public async Task<TenantResponse> UpdateTenantAsync(TenantRequest tenant) => await PutResponseAsync<TenantRequest, TenantResponse>(apiUri, tenant);
        public async Task DeleteTenantAsync(string name) => await DeleteAsync(apiUri, name);

        public async Task<UsageLogResponse> GetUsageLogAsync(UsageTenantLogRequest usageLogRequest) => await GetAsync<UsageTenantLogRequest, UsageLogResponse>(logUsageApiUri, usageLogRequest);
    }
}
