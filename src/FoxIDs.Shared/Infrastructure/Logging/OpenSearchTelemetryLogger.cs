using System.Collections.Generic;
using System;
using ITfoxtec.Identity;
using OpenSearch.Client;
using Amazon.Runtime.Internal.Transform;
using FoxIDs.Models.Config;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;

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
        }

        public void Warning(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            var utcNow = DateTimeOffset.UtcNow;
            Index(utcNow, GetExceptionTelemetryLogString(utcNow, exception, message, properties), "warning");
        }

        public void Error(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            var utcNow = DateTimeOffset.UtcNow;
            Index(utcNow, GetExceptionTelemetryLogString(utcNow, exception, message, properties), "error");
        }

        public void CriticalError(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            var utcNow = DateTimeOffset.UtcNow;
            Index(utcNow, GetExceptionTelemetryLogString(utcNow, exception, message, properties), "critical");
        }

        public void Event(string eventName, IDictionary<string, string> properties = null)
        {
            var utcNow = DateTimeOffset.UtcNow;
            Index(utcNow, GetEventTelemetryLogString(utcNow, eventName, properties), "event");
        }

        public void Trace(string message, IDictionary<string, string> properties = null)
        {
            var utcNow = DateTimeOffset.UtcNow;
            Index(utcNow, GetTraceTelemetryLogString(utcNow, message, properties), "trace");
        }

        public void Metric(string metricName, double value, IDictionary<string, string> properties = null)
        {
            var utcNow = DateTimeOffset.UtcNow;
            Index(utcNow, GetMetricTelemetryLogString(utcNow, metricName, value, properties), "metric");
        }

        private void Index(DateTimeOffset utcNow, IDictionary<string, string> document, string indexName)
        {
            var response = openSearchClient.Index(document, i => i.Index(GetIndexName(utcNow, indexName)));
            if(!response.IsValid)
            {
                try
                {
                    throw new Exception($"OpenSearch log error, Index name '{indexName}'. {response.ServerError}");
                }
                catch (Exception ex)
                {
                    try
                    {
                        stdoutTelemetryLogger.Error(ex);
                    }
                    catch 
                    { }
                }               
            }
        }

        private string GetIndexName(DateTimeOffset utcNow, string logIndexName)
        {
            var lifetime = GetLifetimeInDays(settings.OpenSearch.LogLifetime);

            var routeBinding = httpContextAccessor.HttpContext.TryGetRouteBinding();
            if (routeBinding?.PlanLogLifetime != null)
            {
                lifetime = GetLifetimeInDays(routeBinding.PlanLogLifetime.Value);
            }

            return $"log-{lifetime}d-{logIndexName}-{utcNow.Year}.{utcNow.Month}.{utcNow.Day}";
        }

        private int GetLifetimeInDays(LogLifetimeOptions logLifetime)
        {
            return logLifetime switch
            {
                LogLifetimeOptions.Max30Days => 30,
                LogLifetimeOptions.Max180Days => 180,
                _ => throw new NotSupportedException(),
            };
        }

        private IDictionary<string, string> GetExceptionTelemetryLogString(DateTimeOffset utcNow, Exception exception, string message, IDictionary<string, string> properties)
        {
            var log = new Dictionary<string, string>
            {
                InitLogString(utcNow)
            };
            if (!message.IsNullOrWhiteSpace())
            {
                log.Add("Message", message);
            }
            if (exception != null)
            {
                log.Add("Exception", exception.ToString());
            }

            return log.ConcatOnce(properties);
        }

        private IDictionary<string, string> GetEventTelemetryLogString(DateTimeOffset utcNow, string eventName, IDictionary<string, string> properties)
        {
            var log = new Dictionary<string, string>
            {
                InitLogString(utcNow),
                { "EventName", eventName }
            };

            return log.ConcatOnce(properties);
        }

        private IDictionary<string, string> GetTraceTelemetryLogString(DateTimeOffset utcNow, string message, IDictionary<string, string> properties)
        {
            var log = new Dictionary<string, string>
            {
                InitLogString(utcNow),
                { "Message", message }
            };

            return log.ConcatOnce(properties);
        }

        private IDictionary<string, string> GetMetricTelemetryLogString(DateTimeOffset utcNow, string metricName, double value, IDictionary<string, string> properties)
        {
            var log = new Dictionary<string, string>
            {
                InitLogString(utcNow),
                { "MetricName", metricName },
                { "Value", value.ToString() }
            };

            return log.ConcatOnce(properties);
        }

        private KeyValuePair<string, string> InitLogString(DateTimeOffset utcNow)
        {
            return new KeyValuePair<string, string>("Timestamps", utcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));
        }
    }
}
