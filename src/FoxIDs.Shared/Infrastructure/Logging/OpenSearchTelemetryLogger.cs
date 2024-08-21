using System.Collections.Generic;
using System;
using ITfoxtec.Identity;
using OpenSearch.Client;
using FoxIDs.Models.Config;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using OpenSearch.Net;
using System.Net;

namespace FoxIDs.Infrastructure.Logging
{
    public class OpenSearchTelemetryLogger
    {
        private readonly Settings settings;
        private readonly OpenSearchClient openSearchClient;
        private readonly StdoutTelemetryLogger stdoutTelemetryLogger;
        private readonly IHttpContextAccessor httpContextAccessor;

        public OpenSearchTelemetryLogger(Settings settings, OpenSearchClient openSearchClient, StdoutTelemetryLogger stdoutTelemetryLogger, IHttpContextAccessor httpContextAccessor)
        {
            this.settings = settings;
            this.openSearchClient = openSearchClient;
            this.stdoutTelemetryLogger = stdoutTelemetryLogger;
            this.httpContextAccessor = httpContextAccessor;
            Init();
        }

        private void Init()
        {
            AddMapping();
            CreateIndexPolicy(LogLifetimeOptions.Max30Days);
            CreateIndexPolicy(LogLifetimeOptions.Max180Days);
        }

        private void AddMapping()
        {
            var policyPath = "_index_template/keyword-template";

            var getResponse = openSearchClient.LowLevel.DoRequest<StringResponse>(HttpMethod.GET, policyPath);
            if (getResponse.HttpStatusCode == (int)HttpStatusCode.NotFound)
            {
                openSearchClient.LowLevel.DoRequest<StringResponse>(HttpMethod.PUT, policyPath,
                     PostData.Serializable(new
                     {
                         index_patterns = new[] { "log*" },
                         template = new
                         {
                             mappings = new
                             {
                                 properties = new 
                                 {
                                    tenantName = new { type = "keyword" },
                                    trackName = new { type = "keyword" }
                                 }
                             }
                         }
                     }));
            }

        }

        private void CreateIndexPolicy(LogLifetimeOptions logLifetime)
        {
            var lifetime = (int)logLifetime;
            var policyPath = $"_plugins/_ism/policies/log-{lifetime}d";
            var indexPattern = $"log-{lifetime}d*";

            var getResponse = openSearchClient.LowLevel.DoRequest<StringResponse>(HttpMethod.GET, policyPath);
            if (getResponse.HttpStatusCode == (int)HttpStatusCode.NotFound)
            {
                openSearchClient.LowLevel.DoRequest<StringResponse>(HttpMethod.PUT, policyPath,
                    PostData.Serializable(new
                    {
                        policy = new
                        {
                            description = $"Index policy with a lifetime of {lifetime} days.",
                            default_state = "write",
                            states = new object[] 
                            {
                                new 
                                {
                                    name = "write",
                                    transitions = new [] 
                                    {
                                        new 
                                        {
                                            state_name = "read",
                                            conditions = new 
                                            {
                                                min_index_age = "1d"
                                            }
                                        }
                                    }
                                },
                                new 
                                {
                                    name = "read",
                                    actions = new [] 
                                    {
                                        new 
                                        {
                                            read_only = new { },
                                            retry = new 
                                            {
                                                count = 3,
                                                backoff = "exponential",
                                                delay = "1m"
                                            }
                                        }
                                    },
                                    transitions = new [] 
                                    {
                                        new 
                                        {
                                            state_name = "delete",
                                            conditions = new 
                                            {
                                                min_index_age = "30d"
                                            }
                                        }
                                    }
                                },
                                new 
                                {
                                    name = "delete",
                                    actions = new []
                                    {
                                        new 
                                        {
                                            delete = new { },
                                            retry = new 
                                            {
                                                count = 3,
                                                backoff = "exponential",
                                                delay = "1m"
                                            }
                                        }
                                    }
                                }
                            },
                            ism_template = new[] 
                            {
                                new 
                                {
                                    index_patterns = new[] 
                                    {
                                        indexPattern
                                    },
                                    priority = 1
                                }
                            }
                        }
                    }));
            }
        }

        public void Warning(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            Index(GetExceptionTelemetryLogString(LogTypes.Warning, exception, message, properties), Constants.Logs.IndexName.Errors);
        }

        public void Error(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            Index(GetExceptionTelemetryLogString(LogTypes.Error, exception, message, properties), Constants.Logs.IndexName.Errors);
        }

        public void CriticalError(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            Index(GetExceptionTelemetryLogString(LogTypes.CriticalError, exception, message, properties), Constants.Logs.IndexName.Errors);
        }

        public void Event(string eventName, IDictionary<string, string> properties = null)
        {
            Index(GetEventTelemetryLogString(eventName, properties), Constants.Logs.IndexName.Events);
        }

        public void Trace(string message, IDictionary<string, string> properties = null)
        {
            Index(GetTraceTelemetryLogString(message, properties), Constants.Logs.IndexName.Traces);
        }

        public void Metric(string metricName, double value, IDictionary<string, string> properties = null)
        {
            Index(GetMetricTelemetryLogString(metricName, value, properties), Constants.Logs.IndexName.Metrics);
        }

        private void Index(OpenSearchLogItem logItem, string indexName)
        {
            try
            {
                openSearchClient.Index(logItem, i => i.Index(GetIndexName(logItem.Timestamp, indexName)));
            }
            catch (Exception ex)
            {
                try
                {
                    stdoutTelemetryLogger.Error(ex, $"OpenSearch log error, Index name '{indexName}'.");
                }
                catch
                { }
            }
        }

        private string GetIndexName(DateTimeOffset utcNow, string logIndexName)
        {
            var lifetime = settings.OpenSearch.LogLifetime.GetLifetimeInDays();

            var routeBinding = httpContextAccessor.HttpContext.TryGetRouteBinding();
            if (routeBinding?.PlanLogLifetime != null)
            {
                lifetime = routeBinding.PlanLogLifetime.Value.GetLifetimeInDays();
            }

            return $"log-{lifetime}d-{logIndexName}-{utcNow.Year}.{utcNow.Month}.{utcNow.Day}";
        }        

        private OpenSearchLogItem GetExceptionTelemetryLogString(LogTypes logType, Exception exception, string message, IDictionary<string, string> properties)
        {
            var logItem = CreateLogItem(logType, properties);
            if (!message.IsNullOrWhiteSpace())
            {
                logItem.Message = message;
            }
            if (exception != null)
            {
                logItem.Details = GetDetails(exception);
            }
            return logItem;           
        }

        private IEnumerable<string> GetDetails(Exception exception)
        {
            yield return $"{exception.GetType().FullName}: {exception.Message}{Environment.NewLine}{exception.StackTrace}";
            if (exception.InnerException != null)
            {
                foreach (var detail in GetDetails(exception.InnerException))
                {
                    yield return detail;
                }
            }
        }

        private OpenSearchLogItem GetEventTelemetryLogString(string eventName, IDictionary<string, string> properties)
        {
            var logItem = CreateLogItem(LogTypes.Event, properties);
            logItem.Message = eventName;
            return logItem;
        }

        private OpenSearchLogItem GetTraceTelemetryLogString(string message, IDictionary<string, string> properties)
        {
            var logItem = CreateLogItem(LogTypes.Trace, properties);
            logItem.Message = message;
            return logItem;
        }

        private OpenSearchLogItem GetMetricTelemetryLogString(string metricName, double value, IDictionary<string, string> properties)
        {
            var logItem = CreateLogItem(LogTypes.Metric, properties);
            logItem.Message = metricName;
            logItem.Value = value;
            return logItem;
        }

        private OpenSearchLogItem CreateLogItem(LogTypes logType, IDictionary<string, string> properties)
        {
            var logItem = CreateLogItem(properties);
            logItem.LogType = logType.ToString();
            logItem.Timestamp = DateTimeOffset.UtcNow;
            return logItem;
        }

        private OpenSearchLogItem CreateLogItem(IDictionary<string, string> properties)
        {
            if(properties != null && properties.Count > 0)
            {
                var json = JsonConvert.SerializeObject(properties);
                return json.ToObject<OpenSearchLogItem>();
            }
            else
            {
                return new OpenSearchLogItem();
            }
        }
    }    
}
