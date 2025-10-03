using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using Azure.Monitor.Query;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FoxIDs.Logic
{
    public class AuditLogLogic : LogicBase
    {
        private const int maxResponseLogItems = 300;
        private readonly FoxIDsControlSettings settings;
        private readonly IServiceProvider serviceProvider;

        public AuditLogLogic(FoxIDsControlSettings settings, IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.serviceProvider = serviceProvider;
        }

        public async Task<Api.LogResponse> GetAuditLogsAsync(Api.AuditLogRequest logRequest, string defaultTenantName, string defaultTrackName)
        {
            if (!logRequest.Filter.IsNullOrWhiteSpace())
            {
                logRequest.Filter = logRequest.Filter.Trim();
            }

            if (!logRequest.AuditType.IsNullOrWhiteSpace())
            {
                logRequest.AuditType = logRequest.AuditType.Trim();
            }
            if (!logRequest.AuditDataType.IsNullOrWhiteSpace())
            {
                logRequest.AuditDataType = logRequest.AuditDataType.Trim();
            }
            if (!logRequest.AuditDataAction.IsNullOrWhiteSpace())
            {
                logRequest.AuditDataAction = logRequest.AuditDataAction.Trim();
            }
            if (!logRequest.DocumentId.IsNullOrWhiteSpace())
            {
                logRequest.DocumentId = logRequest.DocumentId.Trim();
            }
            if (!logRequest.PartitionId.IsNullOrWhiteSpace())
            {
                logRequest.PartitionId = logRequest.PartitionId.Trim();
            }
            if (!logRequest.Data.IsNullOrWhiteSpace())
            {
                logRequest.Data = logRequest.Data.Trim();
            }

            var tenantName = logRequest.TenantName.IsNullOrWhiteSpace() ? defaultTenantName : logRequest.TenantName.Trim();
            var trackName = logRequest.TrackName.IsNullOrWhiteSpace() ? defaultTrackName : logRequest.TrackName.Trim();

            var start = DateTimeOffset.FromUnixTimeSeconds(logRequest.FromTime);
            var end = DateTimeOffset.FromUnixTimeSeconds(logRequest.ToTime);

            switch (settings.Options.Log)
            {
                case LogOptions.OpenSearchAndStdoutErrors:
                    return await serviceProvider.GetService<AuditLogOpenSearchLogic>().QueryLogsAsync(logRequest, tenantName, trackName, (start.UtcDateTime, end.UtcDateTime), maxResponseLogItems);
                case LogOptions.ApplicationInsights:
                    return await serviceProvider.GetService<AuditLogApplicationInsightsLogic>().QueryLogsAsync(logRequest, tenantName, trackName, new QueryTimeRange(start, end), maxResponseLogItems);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
