using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using ITfoxtec.Identity;

namespace FoxIDs.Infrastructure
{
    public class StdoutTelemetryLogger
    {
        private readonly ILogger<StdoutTelemetryLogger> logger;

        public StdoutTelemetryLogger()
        {
            logger = GetLogger();
        }

        public void Warning(Exception exception, string message = null, IDictionary<string, string> properties = null)
        {
            logger.LogWarning(GetExceptionTelemetryLogString(exception, message, properties));
        }

        public void Error(Exception exception, string message = null, IDictionary<string, string> properties = null)
        {
            logger.LogError(GetExceptionTelemetryLogString(exception, message, properties));
        }

        public void CriticalError(Exception exception, string message = null, IDictionary<string, string> properties = null)
        {
            logger.LogCritical(GetExceptionTelemetryLogString(exception, message, properties));
        }

        public void Event(string eventName, IDictionary<string, string> properties = null)
        {
            logger.LogInformation(GetEventTelemetryLogString(eventName, properties));
        }

        public void Trace(string message, IDictionary<string, string> properties = null)
        {
            logger.LogTrace(GetTraceTelemetryLogString(message, properties));
        }

        public void Metric(string metricName, double value, IDictionary<string, string> properties = null)
        {
            logger.LogInformation(GetMetricTelemetryLogString(metricName, value, properties));
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
            var log = new List<string>
            {
                $"Timestamp: {DateTimeOffset.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'")}",

            };
            return log;
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

        private ILogger<StdoutTelemetryLogger> GetLogger()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                .AddFilter((f) => true)
                .AddConsole();
            });
            return loggerFactory.CreateLogger<StdoutTelemetryLogger>();
        }
    }
}
