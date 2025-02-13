using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using FoxIDs.Models.Config;
using System.Collections.Generic;
using System.Linq;
using System;
using ITfoxtec.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure;

namespace FoxIDs.Logic
{
    public class LogApplicationInsightsLogic : LogicBase
    {
        private const int maxQueryLogItems = 200;
        private readonly FoxIDsControlSettings settings;
        private readonly LogAnalyticsWorkspaceProvider logAnalyticsWorkspaceProvider;

        public LogApplicationInsightsLogic(FoxIDsControlSettings settings, LogAnalyticsWorkspaceProvider logAnalyticsWorkspaceProvider, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logAnalyticsWorkspaceProvider = logAnalyticsWorkspaceProvider;
        }

        public async Task<Api.LogResponse> QueryLogsAsync(Api.LogRequest logRequest, string tenantName, string trackName, QueryTimeRange queryTimeRange, int maxResponseLogItems)
        {
            var responseTruncated = false;
            var items = new List<InternalLogItem>();
            if (logRequest.QueryExceptions)
            {
                if (await LoadExceptionsAsync(tenantName, trackName, items, queryTimeRange, logRequest.Filter))
                {
                    responseTruncated = true;
                }
            }
            if (logRequest.QueryTraces)
            {
                if (await LoadTracesAsync(tenantName, trackName, items, queryTimeRange, logRequest.Filter))
                {
                    responseTruncated = true;
                }
            }
            if (logRequest.QueryEvents)
            {
                if (await LoadEventsAsync(tenantName, trackName, items, queryTimeRange, logRequest.Filter))
                {
                    responseTruncated = true;
                }
            }
            if (logRequest.QueryMetrics)
            {
                if (await LoadMetricsAsync(tenantName, trackName, items, queryTimeRange, logRequest.Filter))
                {
                    responseTruncated = true;
                }
            }

            if (items.Count() >= maxResponseLogItems)
            {
                responseTruncated = true;
            }

            var orderedItems = items.OrderBy(i => i.Timestamp).Take(maxResponseLogItems).Select(i => ToApiLogItem(i));

            var logResponse = new Api.LogResponse { Items = new List<Api.LogItem>(), ResponseTruncated = responseTruncated };
            foreach (var item in orderedItems)
            {
                if (!string.IsNullOrEmpty(item.SequenceId))
                {
                    var sequenceItem = logResponse.Items.Where(i => i.Type == Api.LogItemTypes.Sequence && i.SequenceId == item.SequenceId).FirstOrDefault();
                    if (sequenceItem == null)
                    {
                        sequenceItem = new Api.LogItem
                        {
                            Timestamp = item.Timestamp,
                            Type = Api.LogItemTypes.Sequence,
                            SequenceId = item.SequenceId,
                            SubItems = new List<Api.LogItem>()
                        };
                        logResponse.Items.Add(sequenceItem);
                    }
                    if (!string.IsNullOrEmpty(item.OperationId))
                    {
                        var operationItem = sequenceItem.SubItems.Where(i => i.Type == Api.LogItemTypes.Operation && i.OperationId == item.OperationId).FirstOrDefault();
                        if (operationItem == null)
                        {
                            operationItem = new Api.LogItem
                            {
                                Timestamp = item.Timestamp,
                                Type = Api.LogItemTypes.Operation,
                                OperationId = item.OperationId,
                                SubItems = new List<Api.LogItem>(),
                                Values = new Dictionary<string, string>()
                            };
                            sequenceItem.SubItems.Add(operationItem);
                        }
                        HandleOperationName(item, operationItem);
                        HandleOperationInfo(item, operationItem);
                        operationItem.SubItems.Add(item);
                    }
                    else
                    {
                        HandleSequenceInfo(item, sequenceItem);
                        sequenceItem.SubItems.Add(item);
                    }
                }
                else if (!string.IsNullOrEmpty(item.OperationId))
                {
                    var operationItem = logResponse.Items.Where(i => i.Type == Api.LogItemTypes.Operation && i.OperationId == item.OperationId).FirstOrDefault();
                    if (operationItem == null)
                    {
                        operationItem = new Api.LogItem
                        {
                            Timestamp = item.Timestamp,
                            Type = Api.LogItemTypes.Operation,
                            OperationId = item.OperationId,
                            SubItems = new List<Api.LogItem>(),
                            Values = new Dictionary<string, string>()
                        };
                        logResponse.Items.Add(operationItem);
                    }
                    HandleOperationName(item, operationItem);
                    HandleOperationInfo(item, operationItem);
                    operationItem.SubItems.Add(item);
                }
                else
                {
                    logResponse.Items.Add(item);
                }
            }

            return logResponse;
        }

        private Api.LogItem ToApiLogItem(InternalLogItem item)
        {
            return new Api.LogItem
            {
                Type = item.Type,
                Timestamp = item.Timestamp.HasValue ? item.Timestamp.Value.ToUnixTimeSeconds() : 0,
                SequenceId = item.SequenceId,
                OperationId = item.OperationId,
                Values = item.Values,
                Details = item.Details,
                SubItems = item.SubItems
            };
        }

        private static void HandleSequenceInfo(Api.LogItem item, Api.LogItem sequenceItem)
        {
            if (sequenceItem.Timestamp > item.Timestamp)
            {
                sequenceItem.Timestamp = item.Timestamp;
            }
            sequenceItem.SequenceId = item.SequenceId;
        }

        private static void HandleOperationInfo(Api.LogItem item, Api.LogItem operationItem)
        {
            if (operationItem.Timestamp > item.Timestamp)
            {
                operationItem.Timestamp = item.Timestamp;
            }
            operationItem.OperationId = item.OperationId;
        }

        private static void HandleOperationName(Api.LogItem item, Api.LogItem operationItem)
        {
            if (item.Values.TryGetValue(Constants.Logs.Results.OperationName, out var itemOperationName))
            {
                if (!operationItem.Values.ContainsKey(Constants.Logs.Results.OperationName))
                {
                    operationItem.Values.Add(Constants.Logs.Results.OperationName, itemOperationName);
                }
                item.Values.Remove(Constants.Logs.Results.OperationName);
            }
        }

        private string GetLogAnalyticsWorkspaceId()
        {
            return settings.ApplicationInsights.WorkspaceId;
        }

        private async Task<bool> LoadExceptionsAsync(string tenantName, string trackName, List<InternalLogItem> items, QueryTimeRange queryTimeRange, string filter)
        {
            var extend = filter.IsNullOrEmpty() ? null : $"| extend RequestId = Properties.RequestId | extend RequestPath = Properties.RequestPath {GetGeneralQueryExtend()}";
            var where = filter.IsNullOrEmpty() ? null : $"| where Details contains '{filter}' or RequestId contains '{filter}' or RequestPath contains '{filter}' or {GetGeneralQueryWhere(filter)}";
            var exceptionsQuery = GetQuery(tenantName, trackName, "AppExceptions", extend, where);
            Response<LogsQueryResult> response = await logAnalyticsWorkspaceProvider.QueryWorkspaceAsync(GetLogAnalyticsWorkspaceId(), exceptionsQuery, queryTimeRange);
            var table = response.Value.Table;

            foreach (var row in table.Rows)
            {
                var item = new InternalLogItem
                {
                    Type = row.GetInt32(Constants.Logs.Results.SeverityLevel) switch
                    {
                        4 => Api.LogItemTypes.CriticalError,
                        3 => Api.LogItemTypes.Error,
                        _ => Api.LogItemTypes.Warning
                    },
                    Timestamp = GetTimestamp(row),
                    SequenceId = GetSequenceId(row),
                    OperationId = GetOperationId(row),
                    Values = GetValues(row, [Constants.Logs.Results.OperationName, Constants.Logs.Results.ClientType, Constants.Logs.Results.ClientIp, Constants.Logs.Results.AppRoleInstance])
                };
                AddExceptionDetails(row, item);
                AddProperties(row, item.Values);
                items.Add(item);
            }

            return table.Rows.Count() >= maxQueryLogItems;
        }

        private async Task<bool> LoadTracesAsync(string tenantName, string trackName, List<InternalLogItem> items, QueryTimeRange queryTimeRange, string filter)
        {
            var extend = filter.IsNullOrEmpty() ? null : GetGeneralQueryExtend();
            var where = filter.IsNullOrEmpty() ? null : $"| where Message contains '{filter}' or {GetGeneralQueryWhere(filter)}";
            var tracesQuery = GetQuery(tenantName, trackName, "AppTraces", extend, where);
            Response<LogsQueryResult> response = await logAnalyticsWorkspaceProvider.QueryWorkspaceAsync(GetLogAnalyticsWorkspaceId(), tracesQuery, queryTimeRange);
            var table = response.Value.Table;

            foreach (var row in table.Rows)
            {
                var item = new InternalLogItem
                {
                    Type = Api.LogItemTypes.Trace,
                    Timestamp = GetTimestamp(row),
                    SequenceId = GetSequenceId(row),
                    OperationId = GetOperationId(row),
                    Values = GetValues(row, [Constants.Logs.Results.OperationName, Constants.Logs.Results.ClientType, Constants.Logs.Results.ClientIp, Constants.Logs.Results.AppRoleInstance])
                };
                AddAddTraceMessage(row, item);
                AddProperties(row, item.Values);
                items.Add(item);
            }

            return table.Rows.Count() >= maxQueryLogItems;
        }

        private async Task<bool> LoadEventsAsync(string tenantName, string trackName, List<InternalLogItem> items, QueryTimeRange queryTimeRange, string filter)
        {
            var extend = filter.IsNullOrEmpty() ? null : GetGeneralQueryExtend();
            var where = $"| where isempty(Properties.f_UsageType){(filter.IsNullOrEmpty() ? String.Empty : $" | where Name contains '{filter}' or {GetGeneralQueryWhere(filter)}")}";
            var eventsQuery = GetQuery(tenantName, trackName, "AppEvents", extend, where);
            Response<LogsQueryResult> response = await logAnalyticsWorkspaceProvider.QueryWorkspaceAsync(GetLogAnalyticsWorkspaceId(), eventsQuery, queryTimeRange);
            var table = response.Value.Table;

            foreach (var row in table.Rows)
            {
                var item = new InternalLogItem
                {
                    Type = Api.LogItemTypes.Event,
                    Timestamp = GetTimestamp(row),
                    SequenceId = GetSequenceId(row),
                    OperationId = GetOperationId(row),
                    Values = GetValues(row, [Constants.Logs.Results.Name, Constants.Logs.Results.OperationName])
                };
                items.Add(item);
            }

            return table.Rows.Count() >= maxQueryLogItems;
        }

        private async Task<bool> LoadMetricsAsync(string tenantName, string trackName, List<InternalLogItem> items, QueryTimeRange queryTimeRange, string filter)
        {
            var extend = filter.IsNullOrEmpty() ? null : GetGeneralQueryExtend();
            var where = filter.IsNullOrEmpty() ? null : $"| where Name contains '{filter}' or {GetGeneralQueryWhere(filter)}";
            var customMetricsQuery = GetQuery(tenantName, trackName, "AppMetrics", extend, where);
            Response<LogsQueryResult> response = await logAnalyticsWorkspaceProvider.QueryWorkspaceAsync(GetLogAnalyticsWorkspaceId(), customMetricsQuery, queryTimeRange);
            var table = response.Value.Table;

            foreach (var row in table.Rows)
            {
                var item = new InternalLogItem
                {
                    Type = Api.LogItemTypes.Metric,
                    Timestamp = GetTimestamp(row),
                    SequenceId = GetSequenceId(row),
                    OperationId = GetOperationId(row),
                    Values = GetValues(row, [Constants.Logs.Results.Name, Constants.Logs.Results.Sum, Constants.Logs.Results.OperationName])
                };
                items.Add(item);
            }

            return table.Rows.Count() >= maxQueryLogItems;
        }

        private string GetGeneralQueryExtend() =>
@$"| extend {Constants.Logs.DownPartyId} = Properties.{Constants.Logs.DownPartyId} 
| extend {Constants.Logs.UpPartyId} = Properties.{Constants.Logs.UpPartyId} 
| extend {Constants.Logs.SessionId} = Properties.{Constants.Logs.SessionId} 
| extend {Constants.Logs.ExternalSessionId} = Properties.{Constants.Logs.ExternalSessionId}
| extend {Constants.Logs.UserId} = Properties.{Constants.Logs.UserId} 
| extend {Constants.Logs.Email} = Properties.{Constants.Logs.Email} 
| extend {Constants.Logs.UserAgent} = Properties.{Constants.Logs.UserAgent}";

        private string GetGeneralQueryWhere(string filter) =>
@$"{Constants.Logs.ClientIP} contains '{filter}' or 
{Constants.Logs.DownPartyId} contains '{filter}' or 
{Constants.Logs.UpPartyId} contains '{filter}' or 
{Constants.Logs.SequenceId} contains '{filter}' or 
{Constants.Logs.SessionId} contains '{filter}' or 
{Constants.Logs.ExternalSessionId} contains '{filter}' or 
{Constants.Logs.UserId} contains '{filter}' or 
{Constants.Logs.Email} contains '{filter}' or 
{Constants.Logs.UserAgent} contains '{filter}'";

        private string GetQuery(string tenantName, string trackName, string fromType, string extend, string where)
        {
            return
@$"{fromType}
| extend {Constants.Logs.TenantName} = Properties.{Constants.Logs.TenantName}
| extend {Constants.Logs.TrackName} = Properties.{Constants.Logs.TrackName}
| extend {Constants.Logs.SequenceId} = Properties.{Constants.Logs.SequenceId} {(extend.IsNullOrEmpty() ? string.Empty : extend)}
| where {Constants.Logs.TenantName} == '{tenantName}' and {Constants.Logs.TrackName} == '{trackName}' {(where.IsNullOrEmpty() ? string.Empty : where)}
| limit {maxQueryLogItems}
| order by TimeGenerated";
        }

        private DateTimeOffset? GetTimestamp(LogsTableRow row)
        {
            var timestamp = row.GetDateTimeOffset(Constants.Logs.Results.TimeGenerated);
            return timestamp;
        }

        private string GetSequenceId(LogsTableRow row)
        {
            return row.GetString(Constants.Logs.SequenceId);
        }

        private string GetOperationId(LogsTableRow row)
        {
            return row.GetString(Constants.Logs.Results.OperationId);
        }

        private Dictionary<string, string> GetValues(LogsTableRow row, string[] keys)
        {
            var values = new Dictionary<string, string>();
            foreach (var key in keys)
            {
                var value = row[key];
                if (value != null)
                {
                    values.Add(key, value.ToString());
                }
            }
            return values;
        }

        private void AddExceptionDetails(LogsTableRow row, InternalLogItem item)
        {
            var details = row.GetString(Constants.Logs.Results.Details);
            if (details != null)
            {
                item.Details = new List<Api.LogItemDetail>();

                var logExceptionDetails = details.ToObject<List<LogExceptionDetail>>();
                foreach (var logExceptionDetail in logExceptionDetails)
                {
                    var logItemDetail = new Api.LogItemDetail
                    {
                        Name = logExceptionDetail.Message
                    };
                    logItemDetail.Details = logExceptionDetail.ParsedStack?.Select(s => s.ToString()).ToList();
                    item.Details.Add(logItemDetail);
                }
            }
        }

        private void AddAddTraceMessage(LogsTableRow row, InternalLogItem item)
        {
            var message = row.GetString(Constants.Logs.Results.Message);
            if (message != null)
            {
                item.Details = new List<Api.LogItemDetail>();

                var logTraceMessage = message.ToObject<List<LogTraceMessageItem>>();
                logTraceMessage.Reverse();
                foreach (var messageItem in logTraceMessage)
                {
                    var logItemDetail = new Api.LogItemDetail
                    {
                        Name = messageItem.Message ?? messageItem.MessageOld,
                    };
                    item.Details.Add(logItemDetail);
                }
            }
        }

        private void AddProperties(LogsTableRow row, IDictionary<string, string> values)
        {
            var Properties = row.GetString(Constants.Logs.Results.Properties);
            if (Properties != null)
            {
                var cdResult = Properties.ToObject<Dictionary<string, string>>();
                var cdValues = cdResult.Where(r => r.Key.StartsWith("f_", StringComparison.Ordinal) || r.Key == Constants.Logs.Results.RequestId || r.Key == Constants.Logs.Results.RequestPath);
                foreach (var cdValue in cdValues)
                {
                    var value = cdValue.Value?.Length > Constants.Logs.Results.PropertiesValueMaxLength ? $"{cdValue.Value.Substring(0, Constants.Logs.Results.PropertiesValueMaxLength)}..." : cdValue.Value;
                    values.Add(cdValue.Key, value);
                }
            }
        }

        private class InternalLogItem
        {
            public Api.LogItemTypes Type { get; set; }

            public DateTimeOffset? Timestamp { get; set; }

            public string SequenceId { get; set; }

            public string OperationId { get; set; }

            public Dictionary<string, string> Values { get; set; }

            public List<Api.LogItemDetail> Details { get; set; }

            public List<Api.LogItem> SubItems { get; set; }
        }
    }
}
