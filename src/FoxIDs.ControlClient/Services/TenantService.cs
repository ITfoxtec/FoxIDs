using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class TenantService : BaseService
    {
        private const string apiUri = "api/{tenant}/master/!tenant";
        private const string tenantsApiUri = "api/{tenant}/master/!tenants";
        private const string usageTenantsApiUri = "api/{tenant}/master/!usagetenants";
        private const string logApiUri = "api/{tenant}/master/!tenantlog";
        private const string logUsageApiUri = "api/{tenant}/master/!tenantlogusage";
        private const string logAuditApiUri = "api/{tenant}/master/!tenantlogaudit";
        private const string usageSettingsApiUri = "api/@master/!usagesettings";
        private const string listUsagesApiUri = "api/{tenant}/master/!usages";
        private const string usageApiUri = "api/{tenant}/master/!usage";
        private const string usageInvoicingActionApiUri = "api/{tenant}/master/!usageinvoicingaction";

        public TenantService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<PaginationResponse<Tenant>> GetTenantsAsync(string filterValue, string paginationToken = null, CancellationToken cancellationToken = default) => await GetListAsync<Tenant>(tenantsApiUri, filterValue, parmValue2: filterValue, parmName2: "filterCustomDomain", paginationToken: paginationToken, cancellationToken: cancellationToken);
        public async Task<PaginationResponse<Tenant>> GetUsageTenantsAsync(string filterValue, string paginationToken = null, CancellationToken cancellationToken = default) => await GetListAsync<Tenant>(usageTenantsApiUri, filterValue, parmValue2: filterValue, parmName2: "filterCustomDomain", paginationToken: paginationToken, cancellationToken: cancellationToken);

        public async Task<TenantResponse> GetTenantAsync(string name, CancellationToken cancellationToken = default) => await GetAsync<TenantResponse>(apiUri, name, cancellationToken: cancellationToken);
        public async Task<TenantResponse> CreateTenantAsync(CreateTenantRequest tenant, CancellationToken cancellationToken = default) => await PostResponseAsync<CreateTenantRequest, TenantResponse>(apiUri, tenant, cancellationToken);
        public async Task<TenantResponse> UpdateTenantAsync(TenantRequest tenant, CancellationToken cancellationToken = default) => await PutResponseAsync<TenantRequest, TenantResponse>(apiUri, tenant, cancellationToken);
        public async Task DeleteTenantAsync(string name, CancellationToken cancellationToken = default) => await DeleteAsync(apiUri, name, cancellationToken: cancellationToken);

        public async Task<LogResponse> GetLogAsync(TenantLogRequest logRequest, CancellationToken cancellationToken = default) => await GetAsync<TenantLogRequest, LogResponse>(logApiUri, logRequest, cancellationToken);
        public async Task<UsageLogResponse> GetUsageLogAsync(UsageTenantLogRequest usageLogRequest, CancellationToken cancellationToken = default) => await GetAsync<UsageTenantLogRequest, UsageLogResponse>(logUsageApiUri, usageLogRequest, cancellationToken);
        public async Task<LogResponse> GetAuditLogAsync(AuditLogRequest auditLogRequest, CancellationToken cancellationToken = default) => await GetAsync<AuditLogRequest, LogResponse>(logAuditApiUri, auditLogRequest, cancellationToken);

        public async Task<UsageSettings> GetUsageSettingsAsync(CancellationToken cancellationToken = default) => await GetAsync<UsageSettings>(usageSettingsApiUri, cancellationToken);
        public async Task<UsageSettings> UpdateUsageSettingsAsync(UsageSettings usageSettings, CancellationToken cancellationToken = default) => await PutResponseAsync<UsageSettings, UsageSettings>(usageSettingsApiUri, usageSettings, cancellationToken);

        public async Task<PaginationResponse<UsedBase>> GetUsagesAsync(string filterValue, int year, int month, string paginationToken = null, CancellationToken cancellationToken = default) => await GetListAsync<UsedBase>(listUsagesApiUri, parmValue1: filterValue, parmValue2: Convert.ToString(year), parmValue3: Convert.ToString(month), parmName1: "filterTenantName", parmName2: "year", parmName3: "month", paginationToken: paginationToken, cancellationToken: cancellationToken);

        public async Task<Used> GetUsageAsync(UsageRequest usageRequest, CancellationToken cancellationToken = default) => await GetAsync<UsageRequest, Used>(usageApiUri, usageRequest, cancellationToken);
        public async Task<Used> CreateUsageAsync(UpdateUsageRequest usageRequest, CancellationToken cancellationToken = default) => await PostResponseAsync<UpdateUsageRequest, Used>(usageApiUri, usageRequest, cancellationToken);
        public async Task<Used> UpdateUsageAsync(UpdateUsageRequest usageRequest, CancellationToken cancellationToken = default) => await PutResponseAsync<UpdateUsageRequest, Used>(usageApiUri, usageRequest, cancellationToken);
        public async Task DeleteUsageAsync(string name, CancellationToken cancellationToken = default) => await DeleteAsync(usageApiUri, name, cancellationToken: cancellationToken);

        public async Task<Used> UsageInvoicingActionAsync(UsageInvoicingAction action, CancellationToken cancellationToken = default) => await PostResponseAsync<UsageInvoicingAction, Used>(usageInvoicingActionApiUri, action, cancellationToken);
    }
}
