using Azure;
using Azure.Core;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class LogAnalyticsWorkspaceProvider
    {
        private readonly TokenCredential tokenCredential;

        public LogAnalyticsWorkspaceProvider(TokenCredential tokenCredential)
        {
            this.tokenCredential = tokenCredential;
        }

        public Task<Response<LogsQueryResult>> QueryWorkspaceAsync(string workspaceId, string query, QueryTimeRange timeRange)
        {
            return GetLogsQueryClient().QueryWorkspaceAsync(workspaceId, query, timeRange);
        }

        private LogsQueryClient GetLogsQueryClient()
        {
            return new LogsQueryClient(tokenCredential);
        }
    }
}