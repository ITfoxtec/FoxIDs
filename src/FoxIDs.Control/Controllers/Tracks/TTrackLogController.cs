using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Azure.Core;
using System.Threading;
using System.Net.Http.Headers;
using FoxIDs.Models.Config;
using Microsoft.Azure.ApplicationInsights.Query.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using ITfoxtec.Identity;

namespace FoxIDs.Controllers
{
    public class TTrackLogController : TenantApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly TokenCredential tokenCredential;

        public TTrackLogController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper, IHttpClientFactory httpClientFactory, TokenCredential tokenCredential) : base(logger)
        {
            this.settings = settings;
            this.logger = logger;
            this.mapper = mapper;
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
            var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessToken());

            var loadLogResponse = new Api.LogResponse { Items = new List<Api.LogItem>() };
            await LoadExceptionsAsync(httpClient, loadLogResponse.Items);
            if (logRequest.TraceInsteadOfEvents == true)
            {
                await LoadTracesAsync(httpClient, loadLogResponse.Items);
            }
            else
            {
                await LoadEventsAsync(httpClient, loadLogResponse.Items);
            }

            var orderedItems = loadLogResponse.Items.OrderByDescending(i => i.Timestamp);

            var logResponse = new Api.LogResponse { Items = new List<Api.LogItem>() };
            foreach (var item in orderedItems)
            {
                if (!string.IsNullOrEmpty(item.SequenceId))
                {
                    var sequenceItem = logResponse.Items.Where(i => i.LogItemType == Api.LogItemTypes.Sequence && i.SequenceId == item.SequenceId).FirstOrDefault();
                    if (sequenceItem == null)
                    {
                        sequenceItem = new Api.LogItem
                        {
                            LogItemType = Api.LogItemTypes.Sequence,
                            SequenceId = item.SequenceId,
                            SubItems = new List<Api.LogItem>()
                        };
                        logResponse.Items.Add(sequenceItem);
                    }
                    if (!string.IsNullOrEmpty(item.OperationId))
                    {
                        var operationItem = sequenceItem.SubItems.Where(i => i.LogItemType == Api.LogItemTypes.Operation && i.OperationId == item.OperationId).FirstOrDefault();
                        if (operationItem == null)
                        {
                            operationItem = new Api.LogItem
                            {
                                LogItemType = Api.LogItemTypes.Operation,
                                OperationId = item.OperationId,
                                SubItems = new List<Api.LogItem>()
                            };
                            sequenceItem.SubItems.Add(operationItem);
                        }
                        operationItem.SubItems.Add(item);
                    }
                    else
                    {
                        sequenceItem.SubItems.Add(item);
                    }
                }
                else if (!string.IsNullOrEmpty(item.OperationId))
                {
                    var operationItem = logResponse.Items.Where(i => i.LogItemType == Api.LogItemTypes.Operation && i.OperationId == item.OperationId).FirstOrDefault();
                    if (operationItem == null)
                    {
                        operationItem = new Api.LogItem
                        {
                            LogItemType = Api.LogItemTypes.Operation,
                            OperationId = item.OperationId,
                            SubItems = new List<Api.LogItem>()
                        };
                        logResponse.Items.Add(operationItem);
                    }
                    operationItem.SubItems.Add(item);
                }
                else
                {
                    logResponse.Items.Add(item);
                }

            }


            //queryResults.Results.

            //var logResponse = new Api.LogResponse
            //{
            //    Items = new List<Api.LogItem>
            //    {
            //        new Api.LogItem
            //        {
            //            LogItemType = Api.LogItemTypes.Sequence,
            //            Values = new Dictionary<string, string> { { "sequenceId", "s1234" } },
            //            SubItems = new List<Api.LogItem> 
            //            {
            //                new Api.LogItem
            //                {
            //                    LogItemType = Api.LogItemTypes.Request,
            //                    Values = new Dictionary<string, string> { { "requestId", "r1234" } },
            //                    SubItems = new List<Api.LogItem>
            //                    {
            //                        new Api.LogItem
            //                        {
            //                            LogItemType = Api.LogItemTypes.Error,
            //                            Values = new Dictionary<string, string> { { "message", "some error message" } },
            //                        }
            //                    }
            //                }
            //            }
            //        },
            //        new Api.LogItem
            //        {
            //            LogItemType = Api.LogItemTypes.Request,
            //            Values = new Dictionary<string, string> { { "requestId", "r333" } },
            //            SubItems = new List<Api.LogItem>
            //            {
            //                new Api.LogItem
            //                {
            //                    LogItemType = Api.LogItemTypes.CriticalError,
            //                    Values = new Dictionary<string, string> { { "message", "some other error message" } },
            //                }
            //            }
            //        }
            //    }
            //};

            //var logResponse = new Api.LogResponse { Items = new List<Api.LogItem>() };
            //logResponse.Items.Add(new Api.LogSequenceItem
            //{
            //    SequenceId = "s1234",
            //    Items = new List<Api.LogItem> 
            //    {
            //        new Api.LogRequestItem
            //        {
            //            RequestId = "r1234",
            //            Items = new List<Api.LogItem>
            //            {
            //                new Api.LogErrorItem(Api.LogItemTypes.Error)
            //                {
            //                    Message = "some error message"
            //                }
            //            }
            //        }
            //    }
            //});
            //logResponse.Items.Add(new Api.LogRequestItem
            //{
            //    RequestId = "r333",
            //    Items = new List<Api.LogItem>
            //            {
            //                new Api.LogErrorItem(Api.LogItemTypes.Error)
            //                {
            //                    Message = "some other error message"
            //                }
            //            }
            //});

            return Ok(logResponse);

            //try
            //{
            //    var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });
            //    if (mTrack.Logging != null && mTrack.Logging.ScopedLogger != null)
            //    {
            //        return Ok(mapper.Map<Api.LogSettings>(mTrack.Logging.ScopedLogger));
            //    }
            //    else
            //    {
            //        return NoContent();
            //    }
            //}
            //catch (CosmosDataException ex)
            //{
            //    if (ex.StatusCode == HttpStatusCode.NotFound)
            //    {
            //        logger.Warning(ex, $"NotFound, Get {nameof(Track)}.{nameof(Track.Logging)}.{nameof(Track.Logging.ScopedLogger)} by track name '{RouteBinding.TrackName}'.");
            //        return NotFound($"{nameof(Track)}.{nameof(Track.Logging)}.{nameof(Track.Logging.ScopedLogger)}", RouteBinding.TrackName);
            //    }
            //    throw;
            //}
        }

        private async Task LoadExceptionsAsync(HttpClient httpClient, List<Api.LogItem> items)
        {
            var exceptionsQuery = GetQuery("exceptions");
            using var response = await httpClient.PostAsFormatJsonAsync(ApplicationInsightsUrl, exceptionsQuery);
            var queryResults = await response.ToObjectAsync<QueryResults>();

            foreach (var result in queryResults.Results)
            {
                var item = new Api.LogItem
                {
                    LogItemType = Convert.ToInt32(result["severityLevel"]?.ToString()) switch
                    {
                        4 => Api.LogItemTypes.CriticalError,
                        3 => Api.LogItemTypes.Error,
                        _ => Api.LogItemTypes.Warning
                    },
                    Timestamp = GetTimestamp(result),
                    SequenceId = GetSequenceId(result),
                    OperationId = GetOperationId(result),
                    Values = new Dictionary<string, string>(result.Where(r => r.Key == "method" || r.Key == "details" || r.Key == "customDimensions" || r.Key == "operation_Name" || r.Key == "application_Version" || r.Key == "client_Type" || r.Key == "client_Ip" || r.Key == "cloud_RoleInstance").Select(r => new KeyValuePair<string, string>(r.Key, r.Value?.ToString())))
                };
                items.Add(item);
            }
        }

        private async Task LoadEventsAsync(HttpClient httpClient, List<Api.LogItem> items)
        {
            var eventsQuery = GetQuery("customEvents");
            using var response = await httpClient.PostAsFormatJsonAsync(ApplicationInsightsUrl, eventsQuery);
            var queryResults = await response.ToObjectAsync<QueryResults>();

            foreach (var result in queryResults.Results)
            {
                var item = new Api.LogItem
                {
                    LogItemType = Api.LogItemTypes.Event,
                    Timestamp = GetTimestamp(result),
                    SequenceId = GetSequenceId(result),
                    OperationId = GetOperationId(result),
                    Values = new Dictionary<string, string>(result.Where(r => r.Key == "name" || r.Key == "operation_Name" || r.Key == "application_Version").Select(r => new KeyValuePair<string, string>(r.Key, r.Value?.ToString())))
                };
                items.Add(item);
            }
        } 
        private async Task LoadTracesAsync(HttpClient httpClient, List<Api.LogItem> items)
        {
            var tracesQuery = GetQuery("traces");
            using var response = await httpClient.PostAsFormatJsonAsync(ApplicationInsightsUrl, tracesQuery);
            var queryResults = await response.ToObjectAsync<QueryResults>();

            foreach (var result in queryResults.Results)
            {
                var item = new Api.LogItem
                {
                    LogItemType = Api.LogItemTypes.Trace,
                    Timestamp = GetTimestamp(result),
                    SequenceId = GetSequenceId(result),
                    OperationId = GetOperationId(result),
                    Values = new Dictionary<string, string>(result.Where(r => r.Key == "message" || r.Key == "customDimensions" || r.Key == "operation_Name" || r.Key == "application_Version" || r.Key == "client_Type" || r.Key == "client_Ip" || r.Key == "cloud_RoleInstance").Select(r => new KeyValuePair<string, string>(r.Key, r.Value?.ToString())))
                };
                items.Add(item);
            }
        }
        private ApplicationInsightsQuery GetQuery(string fromType)
        {
            return new ApplicationInsightsQuery
            {
                Query =
@$"{fromType}
| extend tenantName = customDimensions.tenantName
| extend trackName = customDimensions.trackName
| extend sequenceId = customDimensions.sequenceId
| where tenantName == '{RouteBinding.TenantName}' and trackName == '{RouteBinding.TrackName}'
| limit 200
| order by timestamp desc"
            };
        }

        private long GetTimestamp(IDictionary<string, object> result)
        {
            var timestamp = result["timestamp"]?.ToString();
            return DateTimeOffset.Parse(timestamp).ToUnixTimeSeconds();
        }

        private string GetSequenceId(IDictionary<string, object> result)
        {
            return result["sequenceId"]?.ToString();
        }

        private string GetOperationId(IDictionary<string, object> result)
        {
            return result["operation_Id"]?.ToString();
        }

        private async Task<string> GetAccessToken()
        {
            var accessToken = await tokenCredential.GetTokenAsync(new TokenRequestContext(new[] { "https://api.applicationinsights.io/.default" }), HttpContext.RequestAborted);
            return accessToken.Token;
        }

        private string ApplicationInsightsUrl => $"https://api.applicationinsights.io/v1/apps/{settings.ApplicationInsights.AppId}/query";
    }
}
