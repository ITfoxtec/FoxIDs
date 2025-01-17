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
using System.Web;

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
                // Remove in about 8 month from now 2025.01.17
                OldCreateIndexPolicy(LogLifetimeOptions.Max30Days);
                OldCreateIndexPolicy(LogLifetimeOptions.Max180Days);
                OldAddTemplate();

                CreateIndexPolicy(LogLifetimeOptions.Max30Days);
                CreateIndexPolicy(LogLifetimeOptions.Max180Days);
                AddTemplateAndIndex(LogLifetimeOptions.Max30Days);
                AddTemplateAndIndex(LogLifetimeOptions.Max180Days);
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

        // Remove in about 8 month from now 2025.01.17
        private void OldAddTemplate()
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
                         },
                         priority = 200
                     }));
            }
        }

        // Remove in about 8 month from now 2025.01.17
        private void OldCreateIndexPolicy(LogLifetimeOptions logLifetime)
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
                                    priority = 200
                                }
                            }
                        }
                    }));
            }
        }

        private string IndexPattern(LogLifetimeOptions logLifetime) => $"{RolloverAlias(logLifetime)}*";

        private string RolloverAlias(LogLifetimeOptions logLifetime) => RolloverAlias((int)logLifetime);
        private string RolloverAlias(int lifetime) => $"{settings.OpenSearch.LogName}-r-{lifetime}d";

        private void AddTemplateAndIndex(LogLifetimeOptions logLifetime)
        {
            var lifetime = (int)logLifetime;
            var templatePath = $"_index_template/{RolloverAlias(logLifetime)}-template";
            ;
            var getResponse = openSearchClient.LowLevel.DoRequest<StringResponse>(HttpMethod.GET, templatePath);
            if (getResponse.HttpStatusCode == (int)HttpStatusCode.NotFound)
            {
                openSearchClient.LowLevel.DoRequest<StringResponse>(HttpMethod.PUT, templatePath, PostData.String(
@$"{{
  ""index_patterns"": [""{IndexPattern(logLifetime)}""],
  ""template"": {{
   ""settings"": {{
    ""index.refresh_interval"": ""5s"",
    ""plugins.index_state_management.rollover_alias"": ""{RolloverAlias(logLifetime)}""
   }},
   ""mappings"": {{
    ""properties"": {{
     ""tenantName"": {{
      ""type"": ""keyword""
     }},
     ""trackName"": {{
      ""type"": ""keyword""
     }},
     ""logType"": {{
      ""type"": ""keyword""
     }}
    }}
   }}
  }},
  ""priority"": 100
}}"));

                openSearchClient.LowLevel.DoRequest<StringResponse>(HttpMethod.PUT, HttpUtility.UrlEncode($"<{RolloverAlias(logLifetime)}-{{now/d}}-000001>"), PostData.String(
@$"{{
    ""aliases"": {{
        ""{RolloverAlias(logLifetime)}"": {{
            ""is_write_index"": true
        }}
    }}
}}"));
            }
        }

        private void CreateIndexPolicy(LogLifetimeOptions logLifetime)
        {
            var policyPath = $"_plugins/_ism/policies/{RolloverAlias(logLifetime)}";
            var rolloverAge = 7;

            var getResponse = openSearchClient.LowLevel.DoRequest<StringResponse>(HttpMethod.GET, policyPath);
            if (getResponse.HttpStatusCode == (int)HttpStatusCode.NotFound)
            {

                openSearchClient.LowLevel.DoRequest<StringResponse>(HttpMethod.PUT, policyPath, PostData.String(
@$"{{
  ""policy"": {{
    ""description"": ""Rollover index policy with a lifetime of {(int)logLifetime} days."",
    ""default_state"": ""rollover"",
    ""states"": [
      {{
        ""name"": ""rollover"",
        ""actions"": [
          {{
            ""rollover"": {{
              ""min_size"": ""20gb"",
              ""min_primary_shard_size"": ""20gb"",
              ""min_index_age"": ""{rolloverAge}d""
            }}
          }}
        ],
        ""transitions"": [
          {{
            ""state_name"": ""delete"",
            ""conditions"": {{
              ""min_index_age"": ""{(int)logLifetime + 1 + rolloverAge}d""
            }}
          }}
        ]
      }},
      {{
        ""name"": ""delete"",
        ""actions"": [
          {{
            ""delete"": {{}}
          }}
        ]
      }}
    ],
    ""ism_template"": {{
      ""index_patterns"": [""{IndexPattern(logLifetime)}""],
      ""priority"": 100
    }}
  }}
}}"));
            }
        }

        public void Warning(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            Index(GetExceptionTelemetryLogString(LogTypes.Warning, exception, message, properties));
        }

        public void Error(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            Index(GetExceptionTelemetryLogString(LogTypes.Error, exception, message, properties));
        }

        public void CriticalError(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            Index(GetExceptionTelemetryLogString(LogTypes.CriticalError, exception, message, properties));
        }

        public void Event(string eventName, IDictionary<string, string> properties = null)
        {
            Index(GetEventTelemetryLogString(eventName, properties));
        }

        public void Trace(string message, IDictionary<string, string> properties = null)
        {
            Index(GetTraceTelemetryLogString(message, properties));
        }

        public void Metric(string metricName, double value, IDictionary<string, string> properties = null)
        {
            Index(GetMetricTelemetryLogString(metricName, value, properties));
        }

        private void Index<T>(T logItem) where T : OpenSearchLogItemBase
        {
            try
            {
                openSearchClient.Index(logItem, i => i.Index(GetIndexName()));
            }
            catch (Exception ex)
            {
                try
                {
                    stdoutTelemetryLogger.Error(ex, $"OpenSearch log type '{logItem.LogType}' index error.");
                }
                catch
                { }
            }
        }

        private string GetIndexName()
        {
            var lifetime = settings.OpenSearch.LogLifetime.GetLifetimeInDays();

            var routeBinding = httpContextAccessor.HttpContext.TryGetRouteBinding();
            if (routeBinding?.PlanLogLifetime != null)
            {
                lifetime = routeBinding.PlanLogLifetime.Value.GetLifetimeInDays();
            }

            return RolloverAlias(lifetime);
        }

        private OpenSearchErrorLogItem GetExceptionTelemetryLogString(LogTypes logType, Exception exception, string message, IDictionary<string, string> properties)
        {
            var logItem = CreateLogItem<OpenSearchErrorLogItem>(logType, properties);
            var messageItems = new List<ErrorMessageItem>();
            if (!message.IsNullOrWhiteSpace())
            {
                messageItems.Add(new ErrorMessageItem { Message = message });
            }
            if (exception != null)
            {
                messageItems.AddRange(GetErrorMessageItems(exception));
            }
            if (messageItems.Count > 0)
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

        private OpenSearchEventLogItem GetEventTelemetryLogString(string eventName, IDictionary<string, string> properties)
        {
            var logItem = CreateLogItem<OpenSearchEventLogItem>(LogTypes.Event, properties);
            logItem.Message = eventName;
            return logItem;
        }

        private OpenSearchTraceLogItem GetTraceTelemetryLogString(string message, IDictionary<string, string> properties)
        {
            var logItem = CreateLogItem<OpenSearchTraceLogItem>(LogTypes.Trace, properties);
            logItem.Message = message;
            return logItem;
        }

        private OpenSearchMetricLogItem GetMetricTelemetryLogString(string metricName, double value, IDictionary<string, string> properties)
        {
            var logItem = CreateLogItem<OpenSearchMetricLogItem>(LogTypes.Metric, properties);
            logItem.Message = metricName;
            logItem.Value = value;
            return logItem;
        }

        private T CreateLogItem<T>(LogTypes logType, IDictionary<string, string> properties) where T : OpenSearchLogItemBase
        {
            var logItem = CreateLogItem<T>(properties);
            logItem.LogType = logType.ToString();
            logItem.Timestamp = DateTimeOffset.UtcNow;
            return logItem;
        }

        private T CreateLogItem<T>(IDictionary<string, string> properties) where T : OpenSearchLogItemBase
        {
            if (properties != null && properties.Count > 0)
            {
                var json = JsonConvert.SerializeObject(properties);
                return json.ToObject<T>();
            }
            else
            {
                return default;
            }
        }
    }
}
