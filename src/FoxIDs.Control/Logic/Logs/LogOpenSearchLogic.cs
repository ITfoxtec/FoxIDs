using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using OpenSearch.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using FoxIDs.Infrastructure.Hosting;

namespace FoxIDs.Logic
{
    public class LogOpenSearchLogic : LogicBase
    {
        private readonly FoxIDsControlSettings settings;
        private readonly OpenSearchClient openSearchClient;

        public LogOpenSearchLogic(FoxIDsControlSettings settings, OpenSearchClientQueryLog openSearchClient, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.openSearchClient = openSearchClient;
        }

        public async Task<Api.LogResponse> QueryLogs(Api.LogRequest logRequest, (DateTime start, DateTime end) queryTimeRange, int maxResponseLogItems)
        {
            var responseTruncated = false;
            var logItems = await LoadLogsAsync(logRequest, queryTimeRange, maxResponseLogItems);

            if (logItems.Count() >= maxResponseLogItems)
            {
                responseTruncated = true;
            }

            var orderedItems = logItems.Select(ToApiLogItem);

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

        private Api.LogItem ToApiLogItem(OpenSearchLogItem item)
        {
            var type = GetLogType(item.LogType);
            return new Api.LogItem
            {
                Type = type,
                Timestamp = item.Timestamp.ToUnixTimeSeconds(),
                SequenceId = item.SequenceId,
                OperationId = item.OperationId,                
                Values = GetValues(type, item),
                Details = GetDetails(type, item)
            };
        }

        private Api.LogItemTypes GetLogType(string logType)
        {
            Api.LogItemTypes aLogType;
            if (!Enum.TryParse(logType, out aLogType))
            {
                throw new Exception($"Value '{logType}' cannot be converted to enum type '{nameof(Api.LogItemTypes)}'.");
            }
            return aLogType;
        }

        private Dictionary<string, string> GetValues(Api.LogItemTypes type, OpenSearchLogItem item)
        {
            var values = new Dictionary<string, string>();
            if(type == Api.LogItemTypes.Trace)
            {
                values.Add(Constants.Logs.Results.Message, item.Message);
            }
            else if (type == Api.LogItemTypes.Event)
            {
                values.Add(Constants.Logs.Results.Name, item.Message);
            }
            else if (type == Api.LogItemTypes.Metric)
            {
                values.Add(Constants.Logs.Results.Name, item.Message);
                values.Add(Constants.Logs.Results.Sum, item.Value.ToString());
            }
            values.Add(nameof(item.MachineName), item.MachineName);
            values.Add(nameof(item.ClientIP), item.ClientIP);
            values.Add(nameof(item.Domain), item.Domain);
            values.Add(nameof(item.UserAgent), item.UserAgent);
            values.Add(nameof(item.RequestId), item.RequestId);
            values.Add(nameof(item.RequestPath), item.RequestPath);
            values.Add(nameof(item.TenantName), item.TenantName);
            values.Add(nameof(item.TrackName), item.TrackName);
            values.Add(nameof(item.GrantType), item.GrantType);
            values.Add(nameof(item.UpPartyId), item.UpPartyId);
            values.Add(nameof(item.UpPartyClientId), item.UpPartyClientId);
            values.Add(nameof(item.UpPartyStatus), item.UpPartyStatus);
            values.Add(nameof(item.DownPartyId), item.DownPartyId);
            values.Add(nameof(item.DownPartyClientId), item.DownPartyClientId);
            values.Add(nameof(item.ExternalSequenceId), item.ExternalSequenceId);
            values.Add(nameof(item.AccountAction), item.AccountAction);
            values.Add(nameof(item.SequenceCulture), item.SequenceCulture);
            values.Add(nameof(item.Issuer), item.Issuer);
            values.Add(nameof(item.Status), item.Status);
            values.Add(nameof(item.SessionId), item.SessionId);
            values.Add(nameof(item.ExternalSessionId), item.ExternalSessionId);
            values.Add(nameof(item.UserId), item.UserId);
            values.Add(nameof(item.Email), item.Email);
            values.Add(nameof(item.FailingLoginCount), item.FailingLoginCount.ToString());
            return values;
        }

        private List<Api.LogItemDetail> GetDetails(Api.LogItemTypes type, OpenSearchLogItem item)
        {
            if(type == Api.LogItemTypes.Warning || type == Api.LogItemTypes.Error || type == Api.LogItemTypes.CriticalError)
            {
                var logItemDetails = new List<Api.LogItemDetail>();
                if (!item.Message.IsNullOrWhiteSpace())
                {
                    logItemDetails.Add(new Api.LogItemDetail { Name = item.Message });
                }
                foreach (var detail in item.Details)
                {
                    var subItems = detail.Split(Environment.NewLine);
                    var logItemDetail = new Api.LogItemDetail { Name = subItems[0] };
                    if (subItems.Length > 1)
                    {
                        logItemDetail.Details = subItems.Skip(1).ToList();
                    }
                    logItemDetails.Add(logItemDetail);
                }

                return logItemDetails;
            }

            return null;           
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

        private async Task<IEnumerable<OpenSearchLogItem>> LoadLogsAsync(Api.LogRequest logRequest, (DateTime start, DateTime end) queryTimeRange, int maxResponseLogItems)
        {
            var response = await openSearchClient.SearchAsync<OpenSearchLogItem>(s => s
                .Index(GetIndexName())
                    .Size(maxResponseLogItems)
                    .Sort(s => s.Descending(f => f.Timestamp))
                    .Query(q => q
                         .Bool(b => GetQuery(b, logRequest, queryTimeRange)))
                    
                );

            return response.Documents;
        }

        private string GetIndexName()
        {
            var lifetime = settings.OpenSearch.LogLifetime.GetLifetimeInDays();

            if (RouteBinding?.PlanLogLifetime != null)
            {
                lifetime = RouteBinding.PlanLogLifetime.Value.GetLifetimeInDays();
            }

            return $"{Constants.Logs.LogName}-{lifetime}d*";
        }

        private IBoolQuery GetQuery(BoolQueryDescriptor<OpenSearchLogItem> boolQuery, Api.LogRequest logRequest, (DateTime start, DateTime end) queryTimeRange)
        {
            boolQuery = boolQuery.Filter(f => f.DateRange(dt => dt.Field(field => field.Timestamp)
                                     .GreaterThanOrEquals(queryTimeRange.start)
                                     .LessThanOrEquals(queryTimeRange.end)));

            boolQuery = boolQuery.Must(m => m
                .Term(t => t.TenantName, RouteBinding.TenantName) && 
                    m.Term(t => t.TrackName, RouteBinding.TrackName) &&
                    m.Match(ma => ma.Field(f => f.LogType).Query(string.Join(' ', GetLogTypes(logRequest))))
                );

            if (!logRequest.Filter.IsNullOrWhiteSpace())
            {
                boolQuery = boolQuery.Must(m => m
                    .MultiMatch(ma => ma.
                        Fields(fs => fs
                            .Field(f => f.Message)
                            .Field(f => f.Details)
                            .Field(f => f.OperationId)
                            .Field(f => f.RequestId)
                            .Field(f => f.RequestPath)
                            .Field(f => f.ClientIP)
                            .Field(f => f.DownPartyId)
                            .Field(f => f.UpPartyId)
                            .Field(f => f.SequenceId)
                            .Field(f => f.SessionId)
                            .Field(f => f.ExternalSessionId)
                            .Field(f => f.UserId)
                            .Field(f => f.Email)
                            .Field(f => f.UserAgent)
                        )
                        .Query(logRequest.Filter))
                    );
            }

            return boolQuery;
        }

        private static IEnumerable<string> GetLogTypes(Api.LogRequest logRequest)
        {
            if (logRequest.QueryExceptions)
            {
                yield return LogTypes.Warning.ToString();
                yield return LogTypes.Error.ToString();
                yield return LogTypes.CriticalError.ToString();
            }
            if (logRequest.QueryEvents)
            {
                yield return LogTypes.Event.ToString();
            }
            if (logRequest.QueryTraces)
            {
                yield return LogTypes.Trace.ToString();
            }
            if (logRequest.QueryMetrics)
            {
                yield return LogTypes.Metric.ToString();
            }
        }
    }
}
