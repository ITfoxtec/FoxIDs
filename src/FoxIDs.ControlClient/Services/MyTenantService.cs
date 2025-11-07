using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading;
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
        
        public async Task<TenantResponse> GetTenantAsync(CancellationToken cancellationToken = default) => await GetAsync<TenantResponse>(apiUri, cancellationToken);
        public async Task<TenantResponse> UpdateTenantAsync(MyTenantRequest tenant, CancellationToken cancellationToken = default) => await PutResponseAsync<MyTenantRequest, TenantResponse>(apiUri, tenant, cancellationToken);
        public async Task DeleteTenantAsync(CancellationToken cancellationToken = default) => await DeleteAsync(apiUri, cancellationToken);

        public async Task<LogResponse> GetLogAsync(MyTenantLogRequest logRequest, CancellationToken cancellationToken = default) => await GetAsync<MyTenantLogRequest, LogResponse>(logApiUri, logRequest, cancellationToken);
        public async Task<UsageLogResponse> GetUsageLogAsync(UsageMyTenantLogRequest usageLogRequest, CancellationToken cancellationToken = default) => await GetAsync<UsageMyTenantLogRequest, UsageLogResponse>(logUsageApiUri, usageLogRequest, cancellationToken);
        public async Task<LogResponse> GetAuditLogAsync(AuditLogRequest auditLogRequest, CancellationToken cancellationToken = default) => await GetAsync<AuditLogRequest, LogResponse>(logAuditApiUri, auditLogRequest, cancellationToken);

        public async Task<MollieFirstPaymentResponse> CreateMollieFirstPaymentAsync(MollieFirstPaymentRequest firstPaymentRequest, CancellationToken cancellationToken = default) => await PostResponseAsync<MollieFirstPaymentRequest, MollieFirstPaymentResponse>(mollieFirstPaymentApiUri, firstPaymentRequest, cancellationToken);
    }
}
