using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net.Http;
using Azure.Core;
using System.Net.Http.Headers;
using FoxIDs.Models.Config;
using Microsoft.Azure.ApplicationInsights.Query.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Globalization;
using ITfoxtec.Identity;

namespace FoxIDs.Controllers
{
    public class TTrackLogController : TenantApiController
    {
        private const int maxQueryLogItems = 200;
        private const int maxResponseLogItems = 300;
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly TokenCredential tokenCredential;

        public TTrackLogController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IHttpClientFactory httpClientFactory, TokenCredential tokenCredential) : base(logger)
        {
            this.settings = settings;
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.tokenCredential = tokenCredential;
        }

        /// <summary>
        /// Get track logs.
        /// </summary>
        /// <returns>Logs.</returns>
        [ProducesResponseType(typeof(Api.LogResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LogResponse>> GetTrackLog(Api.LogRequest logRequest)
        {
            if (!await ModelState.TryValidateObjectAsync(logRequest)) return BadRequest(ModelState);

            if (!logRequest.QueryExceptions && !logRequest.QueryTraces && !logRequest.QueryEvents && ! logRequest.QueryMetrics)
            {
                logRequest.QueryExceptions = true;
                logRequest.QueryEvents = true;
            }

            var httpClient = httpClientFactory.CreateClient(nameof(HttpClient));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.TokenTypes.Bearer, await GetAccessToken());

            var from = DateTimeOffset.FromUnixTimeSeconds(logRequest.FromTime);
            var to = DateTimeOffset.FromUnixTimeSeconds(logRequest.ToTime);

            var responseTruncated = false;
            var items = new List<Api.LogItem>();
            if (logRequest.QueryExceptions)
            {
                if (await LoadExceptionsAsync(httpClient, items, from, to, logRequest.Filter))
                {
                    responseTruncated = true;
                }
            }
            if (logRequest.QueryTraces)
            {
                if (await LoadTracesAsync(httpClient, items, from, to, logRequest.Filter))
                {
                    responseTruncated = true;
                }
            }
            if (logRequest.QueryEvents)
            {
                if (await LoadEventsAsync(httpClient, items, from, to, logRequest.Filter))
                {
                    responseTruncated = true;
                }
            }
            if (logRequest.QueryMetrics)
            {
                if (await LoadMetricsAsync(httpClient, items, from, to, logRequest.Filter))
                {
                    responseTruncated = true;
                }
            }

            if (items.Count() >= maxResponseLogItems)
            {
                responseTruncated = true;
            }

            var orderedItems = items.OrderBy(i => i.Timestamp).Take(maxResponseLogItems);

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
                        HandleOperationTimestamp(item, operationItem);
                        operationItem.SubItems.Add(item);
                    }
                    else
                    {
                        HandleSequenceTimestamp(item, sequenceItem);
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
                    HandleOperationTimestamp(item, operationItem);
                    operationItem.SubItems.Add(item);
                }
                else
                {
                    logResponse.Items.Add(item);
                }
            }

            return Ok(logResponse);
        }

        private static void HandleSequenceTimestamp(Api.LogItem item, Api.LogItem sequenceItem)
        {
            if (sequenceItem.Timestamp > item.Timestamp)
            {
                sequenceItem.Timestamp = item.Timestamp;
            }
        }

        private static void HandleOperationTimestamp(Api.LogItem item, Api.LogItem operationItem)
        {
            if (operationItem.Timestamp > item.Timestamp)
            {
                operationItem.Timestamp = item.Timestamp;
            }
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

        private async Task<bool> LoadExceptionsAsync(HttpClient httpClient, List<Api.LogItem> items, DateTimeOffset from, DateTimeOffset to, string filter)
        {
            var extend = filter.IsNullOrEmpty() ? null : $"| extend requestId = customDimensions.RequestId | extend requestPath = customDimensions.RequestPath {GetGeneralQueryExtend()}";
            var where = filter.IsNullOrEmpty() ? null : $"| where details contains '{filter}' or requestId contains '{filter}' or requestPath contains '{filter}' or {GetGeneralQueryWhere(filter)}";
            var exceptionsQuery = GetQuery("exceptions", from, to, extend, where);

            using var response = await httpClient.PostAsFormatJsonAsync(ApplicationInsightsUrl, exceptionsQuery);
            var queryResults = await response.ToObjectAsync<QueryResults>();

            foreach (var result in queryResults.Results)
            {
                var item = new Api.LogItem
                {
                    Type = Convert.ToInt32(result[Constants.Logs.Results.SeverityLevel]?.ToString()) switch
                    {
                        4 => Api.LogItemTypes.CriticalError,
                        3 => Api.LogItemTypes.Error,
                        _ => Api.LogItemTypes.Warning
                    },
                    Timestamp = GetTimestamp(result),
                    SequenceId = GetSequenceId(result),
                    OperationId = GetOperationId(result),
                    Values = new Dictionary<string, string>(result.Where(r => r.Key == Constants.Logs.Results.OperationName || r.Key == Constants.Logs.Results.ClientType || r.Key == Constants.Logs.Results.ClientIp || r.Key == Constants.Logs.Results.CloudRoleInstance)
                        .Select(r => new KeyValuePair<string, string>(r.Key, r.Value?.ToString())))
                };
                AddExceptionDetails(result, item);
                AddCustomDimensions(result, item.Values);
                items.Add(item);
            }

            return queryResults.Results.Count() >= maxQueryLogItems;
        }

        private async Task<bool> LoadTracesAsync(HttpClient httpClient, List<Api.LogItem> items, DateTimeOffset from, DateTimeOffset to, string filter)
        {
            var extend = filter.IsNullOrEmpty() ? null : GetGeneralQueryExtend();
            var where = filter.IsNullOrEmpty() ? null : $"| where message contains '{filter}' or {GetGeneralQueryWhere(filter)}";
            var tracesQuery = GetQuery("traces", from, to, extend, where);
            using var response = await httpClient.PostAsFormatJsonAsync(ApplicationInsightsUrl, tracesQuery);
            var queryResults = await response.ToObjectAsync<QueryResults>();

            foreach (var result in queryResults.Results)
            {
                var item = new Api.LogItem
                {
                    Type = Api.LogItemTypes.Trace,
                    Timestamp = GetTimestamp(result),
                    SequenceId = GetSequenceId(result),
                    OperationId = GetOperationId(result),
                    Values = new Dictionary<string, string>(result.Where(r => r.Key == Constants.Logs.Results.OperationName || r.Key == Constants.Logs.Results.ClientType || r.Key == Constants.Logs.Results.ClientIp || r.Key == Constants.Logs.Results.CloudRoleInstance)
                        .Select(r => new KeyValuePair<string, string>(r.Key, r.Value?.ToString())))
                };
                AddAddTraceMessage(result, item);
                AddCustomDimensions(result, item.Values);
                items.Add(item);
            }

            return queryResults.Results.Count() >= maxQueryLogItems;
        }

        private async Task<bool> LoadEventsAsync(HttpClient httpClient, List<Api.LogItem> items, DateTimeOffset from, DateTimeOffset to, string filter)
        {
            var extend = filter.IsNullOrEmpty() ? null : GetGeneralQueryExtend();
            var where = filter.IsNullOrEmpty() ? null : $"| where name contains '{filter}' or {GetGeneralQueryWhere(filter)}";
            var eventsQuery = GetQuery("customEvents", from, to, extend, where);
            using var response = await httpClient.PostAsFormatJsonAsync(ApplicationInsightsUrl, eventsQuery);
            var queryResults = await response.ToObjectAsync<QueryResults>();

            foreach (var result in queryResults.Results)
            {
                var item = new Api.LogItem
                {
                    Type = Api.LogItemTypes.Event,
                    Timestamp = GetTimestamp(result),
                    SequenceId = GetSequenceId(result),
                    OperationId = GetOperationId(result),
                    Values = new Dictionary<string, string>(result.Where(r => r.Key == Constants.Logs.Results.Name || r.Key == Constants.Logs.Results.OperationName).Select(r => new KeyValuePair<string, string>(r.Key, r.Value?.ToString())))
                };
                items.Add(item);
            }

            return queryResults.Results.Count() >= maxQueryLogItems;
        }

        private async Task<bool> LoadMetricsAsync(HttpClient httpClient, List<Api.LogItem> items, DateTimeOffset from, DateTimeOffset to, string filter)
        {
            var extend = filter.IsNullOrEmpty() ? null : GetGeneralQueryExtend();
            var where = filter.IsNullOrEmpty() ? null : $"| where name contains '{filter}' or {GetGeneralQueryWhere(filter)}";
            var eventsQuery = GetQuery("customMetrics", from, to, extend, where);
            using var response = await httpClient.PostAsFormatJsonAsync(ApplicationInsightsUrl, eventsQuery);
            var queryResults = await response.ToObjectAsync<QueryResults>();

            foreach (var result in queryResults.Results)
            {
                var item = new Api.LogItem
                {
                    Type = Api.LogItemTypes.Metrics,
                    Timestamp = GetTimestamp(result),
                    SequenceId = GetSequenceId(result),
                    OperationId = GetOperationId(result),
                    Values = new Dictionary<string, string>(result.Where(r => r.Key == Constants.Logs.Results.Name || r.Key == Constants.Logs.Results.Value || r.Key == Constants.Logs.Results.OperationName).Select(r => new KeyValuePair<string, string>(r.Key, r.Value?.ToString())))
                };
                items.Add(item);
            }

            return queryResults.Results.Count() >= maxQueryLogItems;
        }

        private string GetGeneralQueryExtend() =>
@"| extend f_DownPartyId = customDimensions.f_DownPartyId 
| extend f_SessionId = customDimensions.f_SessionId 
| extend f_ExternalSessionId = customDimensions.f_ExternalSessionId 
| extend f_UserId = customDimensions.f_UserId 
| extend f_Email = customDimensions.f_Email 
| extend f_UserAgent = customDimensions.f_UserAgent";

        private string GetGeneralQueryWhere(string filter) =>
@$"client_IP contains '{filter}' or 
f_DownPartyId contains '{filter}' or 
f_SessionId contains '{filter}' or 
f_ExternalSessionId contains '{filter}' or 
f_UserId contains '{filter}' or 
f_Email contains '{filter}' or 
f_UserAgent contains '{filter}'";

        private ApplicationInsightsQuery GetQuery(string fromType, DateTimeOffset from, DateTimeOffset to, string extend, string where)
        {
            return new ApplicationInsightsQuery
            {
                Query =
@$"{fromType}
| extend f_TenantName = customDimensions.f_TenantName
| extend f_TrackName = customDimensions.f_TrackName
| extend f_SequenceId = customDimensions.f_SequenceId {(extend.IsNullOrEmpty() ? string.Empty : extend)}
| where f_TenantName == '{RouteBinding.TenantName}' and f_TrackName == '{RouteBinding.TrackName}'
| where timestamp between(datetime('{ToUtcString(from)}') .. datetime('{ToUtcString(to)}')) {(where.IsNullOrEmpty() ? string.Empty : where)}
| limit {maxQueryLogItems}
| order by timestamp"
            };
        }

        private string ToUtcString(DateTimeOffset time)
        {
            return time.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        }

        private long GetTimestamp(IDictionary<string, object> result)
        {
            var timestamp = result[Constants.Logs.Results.Timestamp]?.ToString();
            return DateTimeOffset.Parse(timestamp).ToUnixTimeSeconds();
        }

        private string GetSequenceId(IDictionary<string, object> result)
        {
            return result[Constants.Logs.SequenceId]?.ToString();
        }

        private string GetOperationId(IDictionary<string, object> result)
        {
            return result[Constants.Logs.Results.Operation_Id]?.ToString();
        }

        private void AddExceptionDetails(IDictionary<string, object> result, Api.LogItem item)
        {
            if (result.TryGetValue(Constants.Logs.Results.Details, out var details))
            {
                item.Details = new List<Api.LogItemDetail>();

                var logExceptionDetails = details.ToString().ToObject<List<LogExceptionDetail>>();
                foreach (var logExceptionDetail in logExceptionDetails)
                {
                    var logItemDetail = new Api.LogItemDetail
                    {
                        Name = logExceptionDetail.Message
                    };
                    logItemDetail.Details = logExceptionDetail.ParsedStack.Select(s => s.ToString()).ToList();
                    item.Details.Add(logItemDetail);
                }
            }
        }

        private void AddAddTraceMessage(IDictionary<string, object> result, Api.LogItem item)
        {
            if (result.TryGetValue(Constants.Logs.Results.Message, out var message))
            {
                item.Details = new List<Api.LogItemDetail>();

                var logTraceMessage = message.ToString().ToObject<List<LogTraceMessage>>();
                foreach (var messageItem in logTraceMessage)
                {
                    var logItemDetail = new Api.LogItemDetail
                    {
                        Name = messageItem.Message
                    };
                    item.Details.Add(logItemDetail);
                }
            }
        }

        private void AddCustomDimensions(IDictionary<string, object> result, IDictionary<string, string> values)
        {
            if (result.TryGetValue(Constants.Logs.Results.CustomDimensions, out var customDimensions))
            {
                var cdResult = customDimensions.ToString().ToObject<Dictionary<string, string>>();
                var cdValues = cdResult.Where(r => r.Key.StartsWith("f_", StringComparison.Ordinal) || r.Key == Constants.Logs.Results.RequestId || r.Key == Constants.Logs.Results.RequestPath);
                foreach (var cdValue in cdValues)
                {
                    var value = cdValue.Value?.Length > Constants.Logs.Results.CustomDimensionsValueMaxLength ? $"{cdValue.Value.Substring(0, Constants.Logs.Results.CustomDimensionsValueMaxLength)}..." : cdValue.Value;
                    values.Add(cdValue.Key, value);
                }
            }
        }

        private async Task<string> GetAccessToken()
        {
            var accessToken = await tokenCredential.GetTokenAsync(new TokenRequestContext(new[] { "https://api.applicationinsights.io/.default" }), HttpContext.RequestAborted);
            return accessToken.Token;
        }

        private string ApplicationInsightsUrl => $"https://api.applicationinsights.io/v1/apps/{settings.ApplicationInsights.AppId}/query";
    }
}
