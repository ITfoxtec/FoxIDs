using FoxIDs.Models.Api;
using FoxIDs.Models.Config;
using Microsoft.AspNetCore.Http;
using OpenSearch.Client;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class LogOpenSearchLogic : LogicBase
    {
        private readonly FoxIDsControlSettings settings;
        private readonly OpenSearchClient openSearchClient;

        public LogOpenSearchLogic(FoxIDsControlSettings settings, OpenSearchClient openSearchClient, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.openSearchClient = openSearchClient;
        }

        public async Task<LogResponse> QueryLogs(LogRequest logRequest, (DateTimeOffset start, DateTimeOffset end) queryTimeRange, int maxQueryLogItems, int maxResponseLogItems)
        {
            throw new NotImplementedException();
        }
    }
}
