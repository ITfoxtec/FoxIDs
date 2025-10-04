using Api = FoxIDs.Models.Api;
using Azure;
using Azure.Monitor.Query.Models;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Monitor.Query;

namespace FoxIDs.Logic
{
    public class AuditLogApplicationInsightsLogic : LogicBase
    {
        private const int maxQueryLogItems = 300;
        private readonly FoxIDsControlSettings settings;
        private readonly LogAnalyticsWorkspaceProvider logAnalyticsWorkspaceProvider;

        public AuditLogApplicationInsightsLogic(FoxIDsControlSettings settings, LogAnalyticsWorkspaceProvider logAnalyticsWorkspaceProvider, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logAnalyticsWorkspaceProvider = logAnalyticsWorkspaceProvider;
        }

        public async Task<Api.LogResponse> QueryLogsAsync(Api.AuditLogRequest logRequest, string tenantName, string trackName, QueryTimeRange queryTimeRange, int maxResponseLogItems)
        {
            var extendClause = BuildExtendClause();
            var whereClause = BuildWhereClause(logRequest, tenantName, trackName);
            var queryBuilder = new StringBuilder();
            queryBuilder.AppendLine("AppEvents");
            if (!extendClause.IsNullOrWhiteSpace())
            {
                queryBuilder.AppendLine(extendClause);
            }
            if (!whereClause.IsNullOrWhiteSpace())
            {
                queryBuilder.AppendLine(whereClause);
            }
            queryBuilder.AppendLine($"| limit {maxQueryLogItems}");
            queryBuilder.AppendLine("| order by TimeGenerated desc");

            Response<LogsQueryResult> response = await logAnalyticsWorkspaceProvider.QueryWorkspaceAsync(GetLogAnalyticsWorkspaceId(), queryBuilder.ToString(), queryTimeRange);
            var table = response.Value.Table;

            var rows = table.Rows.Take(maxResponseLogItems).ToList();
            var items = rows.Select(ToApiLogItem).Where(item => item != null).ToList();

            return new Api.LogResponse
            {
                Items = items,
                ResponseTruncated = table.Rows.Count() >= maxResponseLogItems
            };
        }

        private Api.LogItem ToApiLogItem(LogsTableRow row)
        {
            var timestamp = row.GetDateTimeOffset(Constants.Logs.Results.TimeGenerated);
            if (timestamp == null)
            {
                return null;
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            AddValue(values, Constants.Logs.Message, row.GetString(Constants.Logs.Results.Message));
            AddValue(values, Constants.Logs.AuditType, row.GetString(Constants.Logs.AuditType));
            AddValue(values, Constants.Logs.AuditDataType, row.GetString(Constants.Logs.AuditDataType));
            AddValue(values, Constants.Logs.AuditDataAction, row.GetString(Constants.Logs.AuditDataAction));
            AddValue(values, Constants.Logs.DocumentId, row.GetString(Constants.Logs.DocumentId));
            AddValue(values, Constants.Logs.Data, row.GetString(Constants.Logs.Data), false);
            AddValue(values, Constants.Logs.UserId, row.GetString(Constants.Logs.UserId));
            AddValue(values, Constants.Logs.Email, row.GetString(Constants.Logs.Email));
            AddValue(values, Constants.Logs.RequestPath, row.GetString(Constants.Logs.RequestPath));
            AddValue(values, Constants.Logs.RequestMethod, row.GetString(Constants.Logs.RequestMethod));
            AddValue(values, Constants.Logs.TenantName, row.GetString(Constants.Logs.TenantName));
            AddValue(values, Constants.Logs.TrackName, row.GetString(Constants.Logs.TrackName));

            return new Api.LogItem
            {
                Type = Api.LogItemTypes.Event,
                Timestamp = timestamp.Value.ToUnixTimeSeconds(),
                SequenceId = row.GetString(Constants.Logs.SequenceId),
                OperationId = row.GetString(Constants.Logs.Results.OperationId),
                Values = values
            };
        }

        private string BuildExtendClause()
        {
            var extends = new List<string>
            {
                $"| extend {Constants.Logs.TenantName} = Properties.{Constants.Logs.TenantName}",
                $"| extend {Constants.Logs.TrackName} = Properties.{Constants.Logs.TrackName}",
                $"| extend {Constants.Logs.SequenceId} = Properties.{Constants.Logs.SequenceId}",
                $"| extend {Constants.Logs.Results.OperationId} = tostring(Properties.{Constants.Logs.Results.OperationId})",
                $"| extend {Constants.Logs.UserId} = Properties.{Constants.Logs.UserId}",
                $"| extend {Constants.Logs.Email} = Properties.{Constants.Logs.Email}",
                $"| extend {Constants.Logs.RequestPath} = Properties.{Constants.Logs.RequestPath}",
                $"| extend {Constants.Logs.RequestMethod} = Properties.{Constants.Logs.RequestMethod}",
                $"| extend {Constants.Logs.AuditType} = tostring(Properties.f_{Constants.Logs.AuditType})",
                $"| extend {Constants.Logs.AuditDataType} = tostring(Properties.f_{Constants.Logs.AuditDataType})",
                $"| extend {Constants.Logs.AuditDataAction} = tostring(Properties.f_{Constants.Logs.AuditDataAction})",
                $"| extend {Constants.Logs.DocumentId} = tostring(Properties.f_{Constants.Logs.DocumentId})",
                $"| extend {Constants.Logs.Data} = tostring(Properties.f_{Constants.Logs.Data})"
            };

            return string.Join(Environment.NewLine, extends);
        }

        private string BuildWhereClause(Api.AuditLogRequest logRequest, string tenantName, string trackName)
        {
            var clauses = new List<string>
            {
                "| where isnotempty(Properties.f_AuditDataAction)"
            };

            if (!tenantName.IsNullOrWhiteSpace())
            {
                clauses.Add($"| where {Constants.Logs.TenantName} == '{EscapeValue(tenantName)}'");
            }

            if (!trackName.IsNullOrWhiteSpace())
            {
                clauses.Add($"| where {Constants.Logs.TrackName} == '{EscapeValue(trackName)}'");
            }

            if (logRequest.AuditType.HasValue)
            {
                clauses.Add($"| where {Constants.Logs.AuditType} == '{EscapeValue(logRequest.AuditType.Value.ToString())}'");
            }

            if (!logRequest.Filter.IsNullOrWhiteSpace())
            {
                var filter = EscapeValue(logRequest.Filter);
                var searchTargets = new[]
                {
                    Constants.Logs.Results.Name,
                    Constants.Logs.Message,
                    Constants.Logs.AuditDataType,
                    Constants.Logs.AuditDataAction,
                    Constants.Logs.DocumentId,
                    Constants.Logs.UserId,
                    Constants.Logs.Email,
                    Constants.Logs.TenantName,
                    Constants.Logs.TrackName,
                    Constants.Logs.RequestPath,
                    Constants.Logs.RequestMethod,
                    Constants.Logs.Data
                };
                var filterClause = string.Join(" or ", searchTargets.Select(t => $"{t} contains '{filter}'"));
                clauses.Add($"| where {filterClause}");
            }

            return string.Join(Environment.NewLine, clauses);
        }

        private string GetLogAnalyticsWorkspaceId() => settings.ApplicationInsights.WorkspaceId;

        private static void AddValue(IDictionary<string, string> values, string key, string value, bool truncate = true)
        {
            if (!value.IsNullOrWhiteSpace())
            {
                value = truncate && value.Length > Constants.Logs.Results.PropertiesValueMaxLength ? $"{value.Substring(0, Constants.Logs.Results.PropertiesValueMaxLength)}..." : value;
                values[key] = value;
            }
        }

        private static string EscapeValue(string value) => value?.Replace("'", "''");
    }
}
