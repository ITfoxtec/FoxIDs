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
        private const string tenantsApiUri = "api/{tenant}/master/!tenants";
        private const string usageTenantsApiUri = "api/{tenant}/master/!usagetenants";
        private const string logUsageApiUri = "api/{tenant}/master/!tenantlogusage";
        private const string usageSettingsApiUri = "api/@master/!usagesettings";
        private const string listUsagesApiUri = "api/{tenant}/master/!usages";
        private const string usageApiUri = "api/{tenant}/master/!usage";
        private const string usageInvoicingActionApiUri = "api/{tenant}/master/!usageinvoicingaction";

        public TenantService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<PaginationResponse<Tenant>> GetTenantsAsync(string filterValue, string paginationToken = null) => await GetListAsync<Tenant>(tenantsApiUri, filterValue, parmValue2: filterValue, parmName2: "filterCustomDomain", paginationToken: paginationToken);
        public async Task<PaginationResponse<Tenant>> GetUsageTenantsAsync(string filterValue, string paginationToken = null) => await GetListAsync<Tenant>(usageTenantsApiUri, filterValue, parmValue2: filterValue, parmName2: "filterCustomDomain", paginationToken: paginationToken);

        public async Task<TenantResponse> GetTenantAsync(string name) => await GetAsync<TenantResponse>(apiUri, name);
        public async Task<TenantResponse> CreateTenantAsync(CreateTenantRequest tenant) => await PostResponseAsync<CreateTenantRequest, TenantResponse>(apiUri, tenant);
        public async Task<TenantResponse> UpdateTenantAsync(TenantRequest tenant) => await PutResponseAsync<TenantRequest, TenantResponse>(apiUri, tenant);
        public async Task DeleteTenantAsync(string name) => await DeleteAsync(apiUri, name);

        public async Task<UsageLogResponse> GetUsageLogAsync(UsageTenantLogRequest usageLogRequest) => await GetAsync<UsageTenantLogRequest, UsageLogResponse>(logUsageApiUri, usageLogRequest);

        public async Task<UsageSettings> GetUsageSettingsAsync() => await GetAsync<UsageSettings>(usageSettingsApiUri);
        public async Task<UsageSettings> UpdateUsageSettingsAsync(UsageSettings usageSettings) => await PutResponseAsync<UsageSettings, UsageSettings>(usageSettingsApiUri, usageSettings);

        public async Task<PaginationResponse<UsedBase>> GetUsagesAsync(string filterValue, int year, int month, string paginationToken = null) => await GetListAsync<UsedBase>(listUsagesApiUri, parmValue1: filterValue, parmValue2: Convert.ToString(year), parmValue3: Convert.ToString(month), parmName1: "filterTenantName", parmName2: "year", parmName3: "month", paginationToken: paginationToken);

        public async Task<Used> GetUsageAsync(UsageRequest usageRequest) => await GetAsync<UsageRequest, Used>(usageApiUri, usageRequest);
        public async Task<Used> CreateUsageAsync(UpdateUsageRequest usageRequest) => await PostResponseAsync<UpdateUsageRequest, Used>(usageApiUri, usageRequest);
        public async Task<Used> UpdateUsageAsync(UpdateUsageRequest usageRequest) => await PutResponseAsync<UpdateUsageRequest, Used>(usageApiUri, usageRequest);
        public async Task DeleteUsageAsync(string name) => await DeleteAsync(usageApiUri, name);

        public async Task<Used> UsageInvoicingActionAsync(UsageInvoicingAction action) => await PostResponseAsync<UsageInvoicingAction, Used>(usageInvoicingActionApiUri, action);
    }
}
