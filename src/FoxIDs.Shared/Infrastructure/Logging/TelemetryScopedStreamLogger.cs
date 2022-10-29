using FoxIDs.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;

namespace FoxIDs.Infrastructure
{
    public class TelemetryScopedStreamLogger
    {
        public void Warning(ScopedStreamLogger scopeStreamLogger, Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            Warning(scopeStreamLogger, exception, null, properties, metrics);
        }
        public void Warning(ScopedStreamLogger scopeStreamLogger, Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            switch (scopeStreamLogger.Type)
            {
                case ScopedStreamLoggerTypes.ApplicationInsights:
                    var telemetryLogger = GetTelemetryLogger(scopeStreamLogger);
                    telemetryLogger.Warning(exception, message, properties: properties, metrics: metrics);
                    break;
                default:
                    throw new NotSupportedException($"Scoped stream logger type '{scopeStreamLogger.Type}' not supported.");
            }
        }

        public void Error(ScopedStreamLogger scopeStreamLogger, Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            Error(scopeStreamLogger, exception, null, properties, metrics);
        }
        public void Error(ScopedStreamLogger scopeStreamLogger, Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            switch (scopeStreamLogger.Type)
            {
                case ScopedStreamLoggerTypes.ApplicationInsights:
                    var telemetryLogger = GetTelemetryLogger(scopeStreamLogger);
                    telemetryLogger.Error(exception, message, properties: properties, metrics: metrics);
                    break;
                default:
                    throw new NotSupportedException($"Scoped stream logger type '{scopeStreamLogger.Type}' not supported.");
            }
        }

        public void CriticalError(ScopedStreamLogger scopeStreamLogger, Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            CriticalError(scopeStreamLogger, exception, null, properties, metrics);
        }
        public void CriticalError(ScopedStreamLogger scopeStreamLogger, Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            switch (scopeStreamLogger.Type)
            {
                case ScopedStreamLoggerTypes.ApplicationInsights:
                    var telemetryLogger = GetTelemetryLogger(scopeStreamLogger);
                    telemetryLogger.CriticalError(exception, message, properties: properties, metrics: metrics);
                    break;
                default:
                    throw new NotSupportedException($"Scoped stream logger type '{scopeStreamLogger.Type}' not supported.");
            }
        }

        public void Event(ScopedStreamLogger scopeStreamLogger, string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            switch (scopeStreamLogger.Type)
            {
                case ScopedStreamLoggerTypes.ApplicationInsights:
                    var telemetryLogger = GetTelemetryLogger(scopeStreamLogger);
                    telemetryLogger.Event(eventName, properties: properties, metrics: metrics);
                    break;
                default:
                    throw new NotSupportedException($"Scoped stream logger type '{scopeStreamLogger.Type}' not supported.");
            }
        }

        public void Trace(ScopedStreamLogger scopeStreamLogger, string message, IDictionary<string, string> properties = null)
        {
            switch (scopeStreamLogger.Type)
            {
                case ScopedStreamLoggerTypes.ApplicationInsights:
                    var telemetryLogger = GetTelemetryLogger(scopeStreamLogger);
                    telemetryLogger.Trace(message, properties: properties);
                    break;
                default:
                    throw new NotSupportedException($"Scoped stream logger type '{scopeStreamLogger.Type}' not supported.");
            }
        }

        public void Metric(ScopedStreamLogger scopeStreamLogger, string message, double value, IDictionary<string, string> properties = null)
        {
            switch (scopeStreamLogger.Type)
            {
                case ScopedStreamLoggerTypes.ApplicationInsights:
                    var telemetryLogger = GetTelemetryLogger(scopeStreamLogger);
                    telemetryLogger.Metric(message, value, properties: properties);
                    break;
                default:
                    throw new NotSupportedException($"Scoped stream logger type '{scopeStreamLogger.Type}' not supported.");
            }
        }

        private TelemetryLogger GetTelemetryLogger(ScopedStreamLogger scopeStreamLogger)
        {
            if (scopeStreamLogger.Type != ScopedStreamLoggerTypes.ApplicationInsights)
            {
                throw new Exception("Not Application Insights scoped stream logger type.");
            }

            var telemetryClient = new TelemetryClient(new TelemetryConfiguration { ConnectionString = scopeStreamLogger.ApplicationInsightsSettings.ConnectionString });
            return new TelemetryLogger(telemetryClient);
        }
    }
}
