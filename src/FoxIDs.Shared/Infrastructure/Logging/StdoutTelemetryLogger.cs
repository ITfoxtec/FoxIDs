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
        private readonly ILogger<TelemetryScopedProcessor> logger;

        public StdoutTelemetryLogger()
        {
            logger = GetLogger();
        }

        public void LogInformation(EventTelemetry eventTelemetry)
        {
            logger.LogInformation(GetEventTelemetryLogString(eventTelemetry));
        }

        public void LogTrace(TraceTelemetry traceTelemetry)
        {
            logger.LogTrace(GetTraceTelemetryLogString(traceTelemetry));
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

        private string GetEventTelemetryLogString(EventTelemetry eventTelemetry)
        {
            var log = new List<string>
            {
                eventTelemetry.Name
            };
            log = AddTelemetry(log, eventTelemetry);
            return string.Join(Environment.NewLine, log);
        }

        private string GetTraceTelemetryLogString(TraceTelemetry traceTelemetry)
        {
            var log = new List<string>
            {
                traceTelemetry.Message
            };
            log = AddTelemetry(log, traceTelemetry);
            return string.Join(Environment.NewLine, log);
        }

        private string GetExceptionTelemetryLogString(ExceptionTelemetry exceptionTelemetry)
        {
            var log = new List<string>();
            if (exceptionTelemetry.Exception == null)
            {
                log.Add(exceptionTelemetry.Message);
            }
            else
            {
                log.Add(exceptionTelemetry.Exception.ToString());
            }
            log = AddTelemetry(log, exceptionTelemetry);
            return string.Join(Environment.NewLine, log);
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

        private ILogger<TelemetryScopedProcessor> GetLogger()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                .AddFilter((f) => true)
                .AddConsole();
            });
            return loggerFactory.CreateLogger<TelemetryScopedProcessor>();
        }
    }
}
