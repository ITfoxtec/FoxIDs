using FoxIDs.Models;
using FoxIDs.Models.Config;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;

namespace FoxIDs.Infrastructure
{
    public class TelemetryScopedStreamLogger
    {
        public void Warning(ScopedStreamLogger scopeStreamLogger, Exception exception, IDictionary<string, string> properties = null)
        {
            Warning(scopeStreamLogger, exception, null, properties);
        }
        public void Warning(ScopedStreamLogger scopeStreamLogger, Exception exception, string message, IDictionary<string, string> properties = null)
        {
            try
            {
                switch (scopeStreamLogger.Type)
                {
                    case ScopedStreamLoggerTypes.ApplicationInsights:
                    {
                        var telemetryLogger = GetTelemetryLogger(scopeStreamLogger);
                        telemetryLogger.Warning(exception, message, properties: properties);
                        ApplicationInsightsFlush(telemetryLogger);
                        break;
                    }
                    default:
                        throw new NotSupportedException($"Scoped stream logger type '{scopeStreamLogger.Type}' not supported.");
                }
            }
            catch
            { }
        }

        public void Error(ScopedStreamLogger scopeStreamLogger, Exception exception, IDictionary<string, string> properties = null)
        {
            Error(scopeStreamLogger, exception, null, properties);
        }
        public void Error(ScopedStreamLogger scopeStreamLogger, Exception exception, string message, IDictionary<string, string> properties = null)
        {
            try
            {
                switch (scopeStreamLogger.Type)
                {
                    case ScopedStreamLoggerTypes.ApplicationInsights:
                    {
                        var telemetryLogger = GetTelemetryLogger(scopeStreamLogger);
                        telemetryLogger.Error(exception, message, properties: properties);
                        ApplicationInsightsFlush(telemetryLogger);
                        break;
                    }
                    default:
                        throw new NotSupportedException($"Scoped stream logger type '{scopeStreamLogger.Type}' not supported.");
                }
            }
            catch
            { }
        }

        public void CriticalError(ScopedStreamLogger scopeStreamLogger, Exception exception, IDictionary<string, string> properties = null)
        {
            CriticalError(scopeStreamLogger, exception, null, properties);
        }
        public void CriticalError(ScopedStreamLogger scopeStreamLogger, Exception exception, string message, IDictionary<string, string> properties = null)
        {
            try
            {
                switch (scopeStreamLogger.Type)
                {
                    case ScopedStreamLoggerTypes.ApplicationInsights:
                    {
                        var telemetryLogger = GetTelemetryLogger(scopeStreamLogger);
                        telemetryLogger.CriticalError(exception, message, properties: properties);
                        ApplicationInsightsFlush(telemetryLogger);
                        break;
                    }
                    default:
                        throw new NotSupportedException($"Scoped stream logger type '{scopeStreamLogger.Type}' not supported.");
                }

            }
            catch
            { }
        }

        public void Event(ScopedStreamLogger scopeStreamLogger, string eventName, IDictionary<string, string> properties = null)
        {
            try
            {
                switch (scopeStreamLogger.Type)
                {
                    case ScopedStreamLoggerTypes.ApplicationInsights:
                    {
                        var telemetryLogger = GetTelemetryLogger(scopeStreamLogger);
                        telemetryLogger.Event(eventName, properties: properties);
                        ApplicationInsightsFlush(telemetryLogger);
                        break;
                    }
                    default:
                        throw new NotSupportedException($"Scoped stream logger type '{scopeStreamLogger.Type}' not supported.");
                }

            }
            catch
            { }
        }

        public void Trace(ScopedStreamLogger scopeStreamLogger, IEnumerable<TraceMessageItem> traceMessages, IDictionary<string, string> properties = null)
        {
            try
            {
                switch (scopeStreamLogger.Type)
                {
                    case ScopedStreamLoggerTypes.ApplicationInsights:
                    {
                        var telemetryLogger = GetTelemetryLogger(scopeStreamLogger);
                        telemetryLogger.Trace(traceMessages, properties: properties);
                        ApplicationInsightsFlush(telemetryLogger);
                        break;
                    }
                    default:
                        throw new NotSupportedException($"Scoped stream logger type '{scopeStreamLogger.Type}' not supported.");
                }
            }
            catch
            { }
        }

        public void Metric(ScopedStreamLogger scopeStreamLogger, string message, double value, IDictionary<string, string> properties = null)
        {
            try
            {
                switch (scopeStreamLogger.Type)
                {
                    case ScopedStreamLoggerTypes.ApplicationInsights:
                    {
                        var telemetryLogger = GetTelemetryLogger(scopeStreamLogger);
                        telemetryLogger.Metric(message, value, properties: properties);
                        ApplicationInsightsFlush(telemetryLogger);
                        break;
                    }
                    default:
                        throw new NotSupportedException($"Scoped stream logger type '{scopeStreamLogger.Type}' not supported.");
                }
            }
            catch
            { }
        }

        private TelemetryLogger GetTelemetryLogger(ScopedStreamLogger scopeStreamLogger)
        {
            if (scopeStreamLogger.Type != ScopedStreamLoggerTypes.ApplicationInsights)
            {
                throw new Exception("Not Application Insights scoped stream logger type.");
            }

            var telemetryClient = new TelemetryClient(new TelemetryConfiguration { ConnectionString = scopeStreamLogger.ApplicationInsightsSettings.ConnectionString });
            return new TelemetryLogger(new Settings { Options = new OptionsSettings { Log = LogOptions.ApplicationInsights } }, null) { ApplicationInsightsTelemetryClient = telemetryClient };
        }

        private static void ApplicationInsightsFlush(TelemetryLogger telemetryLogger)
        {
            if (telemetryLogger.ApplicationInsightsTelemetryClient != null)
            {
                telemetryLogger.ApplicationInsightsTelemetryClient.Flush();
            }
        }
    }
}
