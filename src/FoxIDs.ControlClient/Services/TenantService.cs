using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System;
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
        private const string filterUsageApiUri = "api/{tenant}/master/!filterusage";
        private const string usageApiUri = "api/{tenant}/master/!usage";

        public TenantService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<IEnumerable<Tenant>> FilterTenantAsync(string filterValue) => await FilterAsync<Tenant>(filterApiUri, filterValue, parmValue2: filterValue, parmName2: "filterCustomDomain");

        public async Task<TenantResponse> GetTenantAsync(string name) => await GetAsync<TenantResponse>(apiUri, name);
        public async Task CreateTenantAsync(CreateTenantRequest tenant) => await PostAsync(apiUri, tenant);
        public async Task<TenantResponse> UpdateTenantAsync(TenantRequest tenant) => await PutResponseAsync<TenantRequest, TenantResponse>(apiUri, tenant);
        public async Task DeleteTenantAsync(string name) => await DeleteAsync(apiUri, name);

        public async Task<IEnumerable<UsedBase>> FilterUsageAsync(string filterValue, int year, int month) => await FilterAsync<UsedBase>(filterUsageApiUri, parmValue1: filterValue, parmValue2: Convert.ToString(year), parmValue3: Convert.ToString(month), parmName1: "filterTenantName", parmName2: "year", parmName3: "month");

        public async Task<Used> GetUsageAsync(UsageRequest usageRequest) => await GetAsync<UsageRequest, Used>(usageApiUri, usageRequest);
        public async Task<Used> CreateUsageAsync(UpdateUsageRequest usageRequest) => await PostResponseAsync<UpdateUsageRequest, Used>(apiUri, usageRequest);
        public async Task<Used> UpdateUsageAsync(UpdateUsageRequest usageRequest) => await PutResponseAsync<UpdateUsageRequest, Used>(apiUri, usageRequest);

        public async Task<UsageLogResponse> GetUsageLogAsync(UsageTenantLogRequest usageLogRequest) => await GetAsync<UsageTenantLogRequest, UsageLogResponse>(logUsageApiUri, usageLogRequest);

    }
}
