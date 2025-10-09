using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class MyTenantService : BaseService
    {
        private const string apiUri = "api/{tenant}/master/!mytenant";
        private const string logApiUri = "api/{tenant}/master/!mytenantlog";
        private const string logUsageApiUri = "api/{tenant}/master/!mytenantlogusage";
        private const string logAuditApiUri = "api/{tenant}/master/!mytenantlogaudit";
        private const string mollieFirstPaymentApiUri = "api/{tenant}/master/!mymolliefirstpayment";              

        public MyTenantService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }
        
        public async Task<TenantResponse> GetTenantAsync() => await GetAsync<TenantResponse>(apiUri);
        public async Task<TenantResponse> UpdateTenantAsync(MyTenantRequest tenant) => await PutResponseAsync<MyTenantRequest, TenantResponse>(apiUri, tenant);
        public async Task DeleteTenantAsync() => await DeleteAsync(apiUri);

        public async Task<LogResponse> GetLogAsync(MyTenantLogRequest logRequest) => await GetAsync<MyTenantLogRequest, LogResponse>(logApiUri, logRequest);
        public async Task<UsageLogResponse> GetUsageLogAsync(UsageMyTenantLogRequest usageLogRequest) => await GetAsync<UsageMyTenantLogRequest, UsageLogResponse>(logUsageApiUri, usageLogRequest);
        public async Task<LogResponse> GetAuditLogAsync(AuditLogRequest auditLogRequest) => await GetAsync<AuditLogRequest, LogResponse>(logAuditApiUri, auditLogRequest);

        public async Task<MollieFirstPaymentResponse> CreateMollieFirstPaymentAsync(MollieFirstPaymentRequest firstPaymentRequest) => await PostResponseAsync<MollieFirstPaymentRequest, MollieFirstPaymentResponse>(mollieFirstPaymentApiUri, firstPaymentRequest);
    }
}
