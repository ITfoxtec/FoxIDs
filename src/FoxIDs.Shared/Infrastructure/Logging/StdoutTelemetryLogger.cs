using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using Microsoft.ApplicationInsights.Channel;
using ITfoxtec.Identity;

namespace FoxIDs.Infrastructure.Logging
{
    public class StdoutTelemetryLogger
    {
        private readonly ILogger<StdoutTelemetryLogger> logger;

        public StdoutTelemetryLogger()
        {
            logger = GetLogger();
        }

        public void LogEvent(EventTelemetry eventTelemetry)
        {
            logger.LogInformation(GetEventTelemetryLogString(eventTelemetry));
        }

        public void LogTrace(TraceTelemetry traceTelemetry)
        {
            logger.LogTrace(GetTraceTelemetryLogString(traceTelemetry));
        }

        public void LogMetric(MetricTelemetry metricTelemetry)
        {
            logger.LogInformation(GetMetricTelemetryLogString(metricTelemetry));
        }

        public void LogWarning(ExceptionTelemetry exceptionTelemetry)
        {
            logger.LogWarning(GetExceptionTelemetryLogString(exceptionTelemetry));
        }

        public void LogError(ExceptionTelemetry exceptionTelemetry)
        {
            logger.LogError(GetExceptionTelemetryLogString(exceptionTelemetry));
        }

        public void LogCritical(ExceptionTelemetry exceptionTelemetry)
        {
            logger.LogCritical(GetExceptionTelemetryLogString(exceptionTelemetry));
        }

        private string GetExceptionTelemetryLogString(ExceptionTelemetry exceptionTelemetry)
        {
            var log = new List<string>
            {
                exceptionTelemetry.Message
            };
            if (exceptionTelemetry.Exception != null)
            {
                log.Add(exceptionTelemetry.Exception.ToString());
            }
            return string.Join(Environment.NewLine, AddTelemetry(log, exceptionTelemetry));
        }

        private string GetEventTelemetryLogString(EventTelemetry eventTelemetry)
        {
            var log = new List<string>
            {
                eventTelemetry.Name
            };
            return string.Join(Environment.NewLine, AddTelemetry(log, eventTelemetry));
        }

        private string GetTraceTelemetryLogString(TraceTelemetry traceTelemetry)
        {
            var log = new List<string>
            {
                traceTelemetry.Message
            };
            return string.Join(Environment.NewLine, AddTelemetry(log, traceTelemetry));
        }

        private string GetMetricTelemetryLogString(MetricTelemetry metricTelemetry)
        {
            var log = new List<string>
            {
                $"Name: {metricTelemetry.Name}",
                $"Value: {metricTelemetry.Sum}"
            };
            return string.Join(Environment.NewLine, AddTelemetry(log, metricTelemetry));
        }

        private List<string> AddTelemetry(List<string> log, ITelemetry eventTelemetry)
        {
            log.Add($"Timestamps: {eventTelemetry.Timestamp.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'")}");
            log.Add($"RoleInstance: {eventTelemetry.Context?.Cloud?.RoleInstance}");
            log.Add($"OperationName: {eventTelemetry.Context?.Operation?.Name}");
            log.Add($"ClientIP: {eventTelemetry.Context?.Location?.Ip}");

            if (eventTelemetry is ISupportProperties supportProperties)
            {
                log.Add($"Properties: {supportProperties.Properties?.ToJson()}");
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
