using FoxIDs.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Threading;

namespace FoxIDs.Infrastructure
{
    public class TelemetryScopedStreamLogger
    {
        public void Warning(TelemetryScopedLogger telemetryScopedLogger, ScopedStreamLogger scopeStreamLogger, Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            Warning(telemetryScopedLogger, scopeStreamLogger, exception, null, properties, metrics);
        }
        public void Warning(TelemetryScopedLogger telemetryScopedLogger, ScopedStreamLogger scopeStreamLogger, Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(new Exception("Test StreamLogger Warning1."), logToScopeStream: false);

            try
            {
                switch (scopeStreamLogger.Type)
                {
                    case ScopedStreamLoggerTypes.ApplicationInsights:
                        if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(new Exception("Test StreamLogger Warning2."), logToScopeStream: false);
                        var telemetryLogger = GetTelemetryLogger(scopeStreamLogger);
                        if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(new Exception("Test StreamLogger Warning3."), logToScopeStream: false);
                        telemetryLogger.Warning(exception, message, properties: properties, metrics: metrics);
                        if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(new Exception("Test StreamLogger Warning4."), logToScopeStream: false);
                        break;
                    default:
                        throw new NotSupportedException($"Scoped stream logger type '{scopeStreamLogger.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(ex, "Unable to log warning to scoped stream logger.", logToScopeStream: false);
            }
        }

        public void Error(TelemetryScopedLogger telemetryScopedLogger, ScopedStreamLogger scopeStreamLogger, Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            Error(telemetryScopedLogger, scopeStreamLogger, exception, null, properties, metrics);
        }
        public void Error(TelemetryScopedLogger telemetryScopedLogger, ScopedStreamLogger scopeStreamLogger, Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(new Exception("Test StreamLogger Error1."), logToScopeStream: false);
            try
            {
                switch (scopeStreamLogger.Type)
                {
                    case ScopedStreamLoggerTypes.ApplicationInsights:
                        if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(new Exception("Test StreamLogger Error2."), logToScopeStream: false);
                        var telemetryLogger = GetTelemetryLogger(scopeStreamLogger);
                        if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(new Exception("Test StreamLogger Error3."), logToScopeStream: false);
                        telemetryLogger.Error(exception, message, properties: properties, metrics: metrics);
                        if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(new Exception("Test StreamLogger Error4."), logToScopeStream: false);
                        break;
                    default:
                        throw new NotSupportedException($"Scoped stream logger type '{scopeStreamLogger.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(ex, "Unable to log error to scoped stream logger.", logToScopeStream: false);
            }
        }

        public void CriticalError(TelemetryScopedLogger telemetryScopedLogger, ScopedStreamLogger scopeStreamLogger, Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            CriticalError(telemetryScopedLogger, scopeStreamLogger, exception, null, properties, metrics);
        }
        public void CriticalError(TelemetryScopedLogger telemetryScopedLogger, ScopedStreamLogger scopeStreamLogger, Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(new Exception("Test StreamLogger CriticalError1."), logToScopeStream: false);

            try
            {
                switch (scopeStreamLogger.Type)
                {
                    case ScopedStreamLoggerTypes.ApplicationInsights:
                        if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(new Exception("Test StreamLogger CriticalError2."), logToScopeStream: false);
                        var telemetryLogger = GetTelemetryLogger(scopeStreamLogger);
                        if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(new Exception("Test StreamLogger CriticalError3."), logToScopeStream: false);
                        telemetryLogger.CriticalError(exception, message, properties: properties, metrics: metrics);
                        if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(new Exception("Test StreamLogger CriticalError4."), logToScopeStream: false);
                        break;
                    default:
                        throw new NotSupportedException($"Scoped stream logger type '{scopeStreamLogger.Type}' not supported.");
                }

            }
            catch (Exception ex)
            {
                if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(ex, "Unable to log critical error to scoped stream logger.", logToScopeStream: false);
            }
        }

        public void Event(TelemetryScopedLogger telemetryScopedLogger, ScopedStreamLogger scopeStreamLogger, string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            try
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
            catch (Exception ex)
            {
                if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(ex, "Unable to log event to scoped stream logger.", logToScopeStream: false);
            }
        }

        public void Trace(TelemetryScopedLogger telemetryScopedLogger, ScopedStreamLogger scopeStreamLogger, string message, IDictionary<string, string> properties = null)
        {
            if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(new Exception("Test StreamLogger Trace1."), logToScopeStream: false);

            try
            {
                switch (scopeStreamLogger.Type)
                {
                    case ScopedStreamLoggerTypes.ApplicationInsights:
                        if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(new Exception("Test StreamLogger Trace2."), logToScopeStream: false);
                        var telemetryLogger = GetTelemetryLogger(scopeStreamLogger);
                        if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(new Exception("Test StreamLogger Trace3."), logToScopeStream: false);
                        telemetryLogger.Trace(message, properties: properties);
                        if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(new Exception("Test StreamLogger Trace4."), logToScopeStream: false);
                        break;
                    default:
                        throw new NotSupportedException($"Scoped stream logger type '{scopeStreamLogger.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(ex, "Unable to log trace to scoped stream logger.", logToScopeStream: false);
            }
        }

        public void Metric(TelemetryScopedLogger telemetryScopedLogger, ScopedStreamLogger scopeStreamLogger, string message, double value, IDictionary<string, string> properties = null)
        {
            try
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
            catch (Exception ex)
            {
                if (telemetryScopedLogger != null) telemetryScopedLogger.Warning(ex, "Unable to log metric to scoped stream logger.", logToScopeStream: false);
            }
        }

        private TelemetryLogger GetTelemetryLogger(ScopedStreamLogger scopeStreamLogger)
        {
            if (scopeStreamLogger.Type != ScopedStreamLoggerTypes.ApplicationInsights)
            {
                throw new Exception("Not Application Insights scoped stream logger type.");
            }

            var telemetryConfiguration = new TelemetryConfiguration { ConnectionString = scopeStreamLogger.ApplicationInsightsSettings.ConnectionString };            
            var telemetryClient = new TelemetryClient(telemetryConfiguration);
            try
            {
                throw new Exception($"Exception test, created...");
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex);
            }
            telemetryClient.TrackTrace("test trace, created");
            telemetryClient.Flush();
            Thread.Sleep(5000);
            return new TelemetryLogger(telemetryClient);
        }
    }
}
