using System.Collections.Generic;
using System;
using ITfoxtec.Identity;
using OpenSearch.Client;
using FoxIDs.Models.Config;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

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
            Index(GetExceptionTelemetryLogString(exception, message, properties), Constants.Logs.IndexName.Warnings);
        }

        public void Error(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            Index(GetExceptionTelemetryLogString(exception, message, properties), Constants.Logs.IndexName.Errors);
        }

        public void CriticalError(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            Index(GetExceptionTelemetryLogString(exception, message, properties), Constants.Logs.IndexName.CriticalErrors);
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

        private void Index(IDictionary<string, string> document, string indexName)
        {
            var json = JsonConvert.SerializeObject(document);
            var logItem = json.ToObject<OpenSearchLogItem>();
            logItem.Timestamp = DateTimeOffset.UtcNow;
            var response = openSearchClient.Index(logItem, i => i.Index(GetIndexName(logItem.Timestamp, indexName)));
            if (!response.IsValid)
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
            var lifetime = settings.OpenSearch.LogLifetime.GetLifetimeInDays();

            var routeBinding = httpContextAccessor.HttpContext.TryGetRouteBinding();
            if (routeBinding?.PlanLogLifetime != null)
            {
                lifetime = routeBinding.PlanLogLifetime.Value.GetLifetimeInDays();
            }

            return $"log-{lifetime}d-{logIndexName}-{utcNow.Year}.{utcNow.Month}.{utcNow.Day}";
        }        

        private IDictionary<string, string> GetExceptionTelemetryLogString(Exception exception, string message, IDictionary<string, string> properties)
        {
            var log = new Dictionary<string, string>();
            if (!message.IsNullOrWhiteSpace())
            {
                log.Add(Constants.Logs.Message, message);
            }
            if (exception != null)
            {
                log.Add(Constants.Logs.Exception, exception.ToString());
            }

            return log.ConcatOnce(properties);
        }

        private IDictionary<string, string> GetEventTelemetryLogString(string eventName, IDictionary<string, string> properties)
        {
            var log = new Dictionary<string, string>
            {
                { Constants.Logs.EventName, eventName }
            };

            return log.ConcatOnce(properties);
        }

        private IDictionary<string, string> GetTraceTelemetryLogString(string message, IDictionary<string, string> properties)
        {
            var log = new Dictionary<string, string>
            {
                { Constants.Logs.Message, message }
            };

            return log.ConcatOnce(properties);
        }

        private IDictionary<string, string> GetMetricTelemetryLogString(string metricName, double value, IDictionary<string, string> properties)
        {
            var log = new Dictionary<string, string>
            {
                { Constants.Logs.MetricName, metricName },
                { Constants.Logs.Value, value.ToString() }
            };

            return log.ConcatOnce(properties);
        }
    }    
}
