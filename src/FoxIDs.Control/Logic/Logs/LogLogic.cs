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
    public class LogLogic : LogicBase
    {
        private const int maxResponseLogItems = 300;
        private readonly FoxIDsControlSettings settings;
        private readonly IServiceProvider serviceProvider;

        public LogLogic(FoxIDsControlSettings settings, IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.serviceProvider = serviceProvider;
        }

        public async Task<Api.LogResponse> QueryLogs(Api.LogRequest logRequest, string tenantName, string trackName)
        {
            if (!logRequest.Filter.IsNullOrEmpty())
            {
                logRequest.Filter = logRequest.Filter.Trim();
            }

            if (!logRequest.QueryExceptions && !logRequest.QueryTraces && !logRequest.QueryEvents && !logRequest.QueryMetrics)
            {
                logRequest.QueryExceptions = true;
                logRequest.QueryEvents = true;
            }

            var start = DateTimeOffset.FromUnixTimeSeconds(logRequest.FromTime);
            var end = DateTimeOffset.FromUnixTimeSeconds(logRequest.ToTime);

            switch (settings.Options.Log)
            {
                case LogOptions.OpenSearchAndStdoutErrors:
                    return await serviceProvider.GetService<LogOpenSearchLogic>().QueryLogs(logRequest, tenantName, trackName, (start.DateTime, end.DateTime), maxResponseLogItems);
                case LogOptions.ApplicationInsights:
                    return await serviceProvider.GetService<LogApplicationInsightsLogic>().QueryLogsAsync(logRequest, tenantName, trackName, new QueryTimeRange(start, end), maxResponseLogItems);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
