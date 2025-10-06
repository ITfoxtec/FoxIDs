using Azure;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Api = FoxIDs.Models.Api;

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
            AddValue(values, Constants.Logs.Results.Name, row.GetString(Constants.Logs.Results.Name));

            AddValue(values, Constants.Logs.MachineName, row.GetString(Constants.Logs.MachineName));
            AddValue(values, Constants.Logs.ClientIP, row.GetString(Constants.Logs.ClientIP));
            AddValue(values, Constants.Logs.SessionId, row.GetString(Constants.Logs.SessionId));
            AddValue(values, Constants.Logs.UserAgent, row.GetString(Constants.Logs.UserAgent));
            AddValue(values, Constants.Logs.UpPartyId, row.GetString(Constants.Logs.UpPartyId));
            AddValue(values, Constants.Logs.TenantName, row.GetString(Constants.Logs.TenantName));
            AddValue(values, Constants.Logs.TrackName, row.GetString(Constants.Logs.TrackName));
            AddValue(values, Constants.Logs.UserId, row.GetString(Constants.Logs.UserId));
            AddValue(values, Constants.Logs.Email, row.GetString(Constants.Logs.Email));
            AddValue(values, Constants.Logs.AuditType, row.GetString(Constants.Logs.AuditType));
            AddValue(values, Constants.Logs.AuditAction, row.GetString(Constants.Logs.AuditAction));
            AddValue(values, Constants.Logs.AuditDataAction, row.GetString(Constants.Logs.AuditDataAction));
            AddValue(values, Constants.Logs.DocumentId, row.GetString(Constants.Logs.DocumentId));
            AddValue(values, Constants.Logs.Data, row.GetString(Constants.Logs.Data), false);

            return new Api.LogItem
            {
                Type = Api.LogItemTypes.Event,
                Timestamp = timestamp.Value.ToUnixTimeSeconds(),
                Values = values
            };
        }

        private string BuildExtendClause()
        {
            var extends = new List<string>
            {
                $"| extend {Constants.Logs.MachineName} = Properties.{Constants.Logs.MachineName}",
                $"| extend {Constants.Logs.ClientIP} = Properties.{Constants.Logs.ClientIP}",
                $"| extend {Constants.Logs.SessionId} = Properties.{Constants.Logs.SessionId}",
                $"| extend {Constants.Logs.UserAgent} = Properties.{Constants.Logs.UserAgent}",
                $"| extend {Constants.Logs.UpPartyId} = Properties.{Constants.Logs.UpPartyId}",
                $"| extend {Constants.Logs.TenantName} = Properties.{Constants.Logs.TenantName}",
                $"| extend {Constants.Logs.TrackName} = Properties.{Constants.Logs.TrackName}",
                $"| extend {Constants.Logs.UserId} = Properties.{Constants.Logs.UserId}",
                $"| extend {Constants.Logs.Email} = Properties.{Constants.Logs.Email}",
                $"| extend {Constants.Logs.AuditType} = Properties.{Constants.Logs.AuditType}",
                $"| extend {Constants.Logs.AuditAction} = Properties.{Constants.Logs.AuditAction}",
                $"| extend {Constants.Logs.AuditDataAction} = Properties.{Constants.Logs.AuditDataAction}",
                $"| extend {Constants.Logs.DocumentId} = Properties.{Constants.Logs.DocumentId}",
                $"| extend {Constants.Logs.Data} = Properties.{Constants.Logs.Data}"
            };

            return string.Join(Environment.NewLine, extends);
        }

        private string BuildWhereClause(Api.AuditLogRequest logRequest, string tenantName, string trackName)
        {
            var clauses = new List<string>
            {
                $"| where isnotempty({Constants.Logs.AuditType})"
            };

            if (!tenantName.IsNullOrWhiteSpace())
            {
                clauses.Add($"| where {Constants.Logs.TenantName} == '{EscapeValue(tenantName)}'");
            }

            if (!trackName.IsNullOrWhiteSpace())
            {
                clauses.Add($"| where {Constants.Logs.TrackName} == '{EscapeValue(trackName)}'");
            }

            var mappedFilter = MapSearchText(logRequest);
            if (!mappedFilter.IsNullOrWhiteSpace())
            {                
                var filter = EscapeValue(mappedFilter);
                var searchTargets = new[]
                {
                    Constants.Logs.Results.Name,
                    Constants.Logs.AuditType,
                    Constants.Logs.AuditAction,
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

        private static string MapSearchText(Api.AuditLogRequest logRequest)
        {
            var filter = logRequest.Filter;
            if (filter.IsNullOrWhiteSpace())
            {
                return filter;
            }

            if (filter.Contains("Change password", StringComparison.OrdinalIgnoreCase) && !filter.Contains("ChangePassword", StringComparison.Ordinal))
            {
                filter = string.Concat(filter, " ChangePassword");
            }

            if (filter.Contains("Create user", StringComparison.OrdinalIgnoreCase) && !filter.Contains("CreateUser", StringComparison.Ordinal))
            {
                filter = string.Concat(filter, " CreateUser");
            }

            return filter;
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
