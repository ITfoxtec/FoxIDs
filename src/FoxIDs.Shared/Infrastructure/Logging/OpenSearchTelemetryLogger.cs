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
using System.Linq;

namespace FoxIDs.Infrastructure
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
            try
            {
                AddMapping();
                CreateIndexPolicy(LogLifetimeOptions.Max30Days);
                CreateIndexPolicy(LogLifetimeOptions.Max180Days);
            }
            catch (Exception ex)
            {
                try
                {
                    stdoutTelemetryLogger.Error(ex, $"OpenSearch init error'.");
                }
                catch
                { }
            }
        }

        private void AddMapping()
        {
            var policyPath = $"_index_template/{settings.OpenSearch.LogName}-template";

            var getResponse = openSearchClient.LowLevel.DoRequest<StringResponse>(HttpMethod.GET, policyPath);
            if (getResponse.HttpStatusCode == (int)HttpStatusCode.NotFound)
            {
                openSearchClient.LowLevel.DoRequest<StringResponse>(HttpMethod.PUT, policyPath,
                     PostData.Serializable(new
                     {
                         index_patterns = new[] { $"{settings.OpenSearch.LogName}*" },
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
            var policyPath = $"_plugins/_ism/policies/{settings.OpenSearch.LogName}-{lifetime}d";
            var indexPattern = $"{settings.OpenSearch.LogName}-{lifetime}d*";

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
                                                min_index_age = $"{lifetime + 1}d"
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

            return $"{settings.OpenSearch.LogName}-{lifetime}d-{logIndexName}-{utcNow.Year}.{utcNow.Month}.{utcNow.Day}";
        }        

        private OpenSearchLogItem GetExceptionTelemetryLogString(LogTypes logType, Exception exception, string message, IDictionary<string, string> properties)
        {
            var logItem = CreateLogItem(logType, properties);
            var messageItems = new List<ErrorMessageItem>();
            if (!message.IsNullOrWhiteSpace())
            {
                messageItems.Add(new ErrorMessageItem { Message = message });
            }
            if (exception != null)
            {
                messageItems.AddRange(GetErrorMessageItems(exception));
            }
            if(messageItems.Count > 0)
            {
                logItem.Message = messageItems.ToJson();
            }
            return logItem;           
        }

        private IEnumerable<ErrorMessageItem> GetErrorMessageItems(Exception exception)
        {
            yield return new ErrorMessageItem { Message = $"{exception.GetType().FullName}: {exception.Message}", StackTrace = exception.StackTrace?.Split(Environment.NewLine)?.Select(s => s.Trim()) };
            if (exception.InnerException != null)
            {
                foreach (var messageItem in GetErrorMessageItems(exception.InnerException))
                {
                    yield return messageItem;
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
