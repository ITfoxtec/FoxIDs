using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;

namespace FoxIDs.Infrastructure
{
    public class TelemetryLogger
    {
        private readonly Settings settings;
        private readonly TelemetryClient telemetryClient;

        public TelemetryLogger(Settings settings, TelemetryClient telemetryClient)
        {
            this.settings = settings;
            this.telemetryClient = telemetryClient;
        }

        public void Warning(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            Warning(exception, null, properties, metrics);
        }
        public void Warning(Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryClient.TrackException(GetExceptionTelemetry(SeverityLevel.Warning, exception, message, properties, metrics));
        }

        public void Error(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            Error(exception, null, properties, metrics);
        }
        public void Error(Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryClient.TrackException(GetExceptionTelemetry(SeverityLevel.Error, exception, message, properties, metrics));
        }

        public void CriticalError(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            CriticalError(exception, null, properties, metrics);
        }
        public void CriticalError(Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryClient.TrackException(GetExceptionTelemetry(SeverityLevel.Critical, exception, message, properties, metrics));
        }

        public void Event(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryClient.TrackEvent(eventName, properties, metrics);
        }
        public void Trace(string message, IDictionary<string, string> properties = null)
        {
            telemetryClient.TrackTrace(message, SeverityLevel.Verbose, properties);
        }

        public void Metric(string message, double value, IDictionary<string, string> properties = null)
        {
            telemetryClient.TrackMetric(message, value, properties);
        }

        private static ExceptionTelemetry GetExceptionTelemetry(SeverityLevel severityLevel, Exception exception, string message, IDictionary<string, string> properties, IDictionary<string, double> metrics)
        {
            var exceptionTelemetry = new ExceptionTelemetry(exception)
            {
                SeverityLevel = severityLevel
            };

            if (!message.IsNullOrEmpty())
            {
                exceptionTelemetry.Message = message;
            }
            
            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    exceptionTelemetry.Properties.Add(prop);
                }
            }
            
            if (metrics != null)
            {
                foreach (var metric in metrics)
                {
                    exceptionTelemetry.Metrics.Add(metric);
                }
            }

            return exceptionTelemetry;
        }
    }
}
