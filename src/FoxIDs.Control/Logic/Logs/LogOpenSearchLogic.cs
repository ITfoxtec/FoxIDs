﻿using FoxIDs.Models;
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
                AddValue(values, Constants.Logs.Results.Message, item.Message);
            }
            else if (type == Api.LogItemTypes.Event)
            {
                AddValue(values, Constants.Logs.Results.Name, item.Message);
            }
            else if (type == Api.LogItemTypes.Metric)
            {
                AddValue(values, Constants.Logs.Results.Name, item.Message);
                AddValue(values, Constants.Logs.Results.Sum, item.Value.ToString());
            }
            AddValue(values, nameof(item.MachineName), item.MachineName);
            AddValue(values, nameof(item.ClientIP), item.ClientIP);
            AddValue(values, nameof(item.Domain), item.Domain);
            AddValue(values, nameof(item.UserAgent), item.UserAgent);
            AddValue(values, nameof(item.RequestId), item.RequestId);
            AddValue(values, nameof(item.RequestPath), item.RequestPath);
            AddValue(values, nameof(item.TenantName), item.TenantName);
            AddValue(values, nameof(item.TrackName), item.TrackName);
            AddValue(values, nameof(item.GrantType), item.GrantType);
            AddValue(values, nameof(item.UpPartyId), item.UpPartyId);
            AddValue(values, nameof(item.UpPartyClientId), item.UpPartyClientId);
            AddValue(values, nameof(item.UpPartyStatus), item.UpPartyStatus);
            AddValue(values, nameof(item.DownPartyId), item.DownPartyId);
            AddValue(values, nameof(item.DownPartyClientId), item.DownPartyClientId);
            AddValue(values, nameof(item.ExternalSequenceId), item.ExternalSequenceId);
            AddValue(values, nameof(item.AccountAction), item.AccountAction);
            AddValue(values, nameof(item.SequenceCulture), item.SequenceCulture);
            AddValue(values, nameof(item.Issuer), item.Issuer);
            AddValue(values, nameof(item.Status), item.Status);
            AddValue(values, nameof(item.SessionId), item.SessionId);
            AddValue(values, nameof(item.ExternalSessionId), item.ExternalSessionId);
            AddValue(values, nameof(item.UserId), item.UserId);
            AddValue(values, nameof(item.Email), item.Email);
            AddValue(values, nameof(item.FailingLoginCount), item.FailingLoginCount.ToString());
            return values;
        }

        private void AddValue(Dictionary<string, string> values, string key, string value)
        {
            if (!value.IsNullOrWhiteSpace())
            {
                value = value.Length > Constants.Logs.Results.PropertiesValueMaxLength ? $"{value.Substring(0, Constants.Logs.Results.PropertiesValueMaxLength)}..." : value;
                values.Add(key, value);
            }
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