using System.Collections.Generic;
using System;
using ITfoxtec.Identity;
using OpenSearch.Client;

namespace FoxIDs.Infrastructure.Logging
{
    public class OpenSearchTelemetryLogger
    {
        private readonly OpenSearchClient openSearchClient;

        public OpenSearchTelemetryLogger(OpenSearchClient openSearchClient)
        {
            this.openSearchClient = openSearchClient;
        }

        public void Warning(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            openSearchClient.Index(GetExceptionTelemetryLogString(exception, message, properties), i => i.Index("Warning"));
        }

        public void Error(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            openSearchClient.Index(GetExceptionTelemetryLogString(exception, message, properties), i => i.Index("Error"));
        }

        public void CriticalError(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            openSearchClient.Index(GetExceptionTelemetryLogString(exception, message, properties), i => i.Index("CriticalError"));
        }

        public void Event(string eventName, IDictionary<string, string> properties = null)
        {
            openSearchClient.Index(GetEventTelemetryLogString(eventName, properties), i => i.Index("Event"));
        }

        public void Trace(string message, IDictionary<string, string> properties = null)
        {
            openSearchClient.Index(GetTraceTelemetryLogString(message, properties), i => i.Index("Trace"));
        }

        public void Metric(string metricName, double value, IDictionary<string, string> properties = null)
        {
            openSearchClient.Index(GetMetricTelemetryLogString(metricName, value, properties), i => i.Index("Metric"));
        }

        private string GetExceptionTelemetryLogString(Exception exception, string message, IDictionary<string, string> properties)
        {
            var log = InitLogString();
            if (!message.IsNullOrWhiteSpace())
            {
                log.Add(message);
            }
            if (exception != null)
            {
                log.Add(exception.ToString());
            }
            return string.Join(Environment.NewLine, AddPropertiesTelemetryLogString(log, properties));
        }

        private string GetEventTelemetryLogString(string eventName, IDictionary<string, string> properties)
        {
            var log = InitLogString();
            log.Add(eventName);
            return string.Join(Environment.NewLine, AddPropertiesTelemetryLogString(log, properties));
        }

        private string GetTraceTelemetryLogString(string message, IDictionary<string, string> properties)
        {
            var log = InitLogString();
            log.Add(message);
            return string.Join(Environment.NewLine, AddPropertiesTelemetryLogString(log, properties));
        }

        private string GetMetricTelemetryLogString(string metricName, double value, IDictionary<string, string> properties)
        {
            var log = InitLogString();
            log.Add(metricName);
            log.Add($"Value: {value}");
            return string.Join(Environment.NewLine, AddPropertiesTelemetryLogString(log, properties));
        }

        private List<string> InitLogString()
        {
            return new List<string>
            {
                $"Timestamps: {DateTimeOffset.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'")}"
            };
        }

        private List<string> AddPropertiesTelemetryLogString(List<string> log, IDictionary<string, string> properties)
        {
            if (properties?.Count > 0)
            {
                foreach (var property in properties)
                {
                    log.Add($"{property.Key}: {property.Value}");
                }
            }
            return log;
        }
    }
}
