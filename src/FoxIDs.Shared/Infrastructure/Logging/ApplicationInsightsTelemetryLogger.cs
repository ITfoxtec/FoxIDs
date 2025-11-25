using ITfoxtec.Identity;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;

namespace FoxIDs.Infrastructure
{
    public class ApplicationInsightsTelemetryLogger
    {
        private readonly TelemetryClient telemetryClient;

        public ApplicationInsightsTelemetryLogger(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
        }

        public void Warning(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            telemetryClient.TrackException(GetApplicationInsightsExceptionTelemetry(SeverityLevel.Warning, exception, message, properties));
        }

        public void Error(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            telemetryClient.TrackException(GetApplicationInsightsExceptionTelemetry(SeverityLevel.Error, exception, message, properties));
        }

        public void CriticalError(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            telemetryClient.TrackException(GetApplicationInsightsExceptionTelemetry(SeverityLevel.Critical, exception, message, properties));
        }

        public void Event(string eventName, IDictionary<string, string> properties = null)
        {
            telemetryClient.TrackEvent(eventName, properties);
        }
        public void Trace(string message, IDictionary<string, string> properties = null)
        {
            telemetryClient.TrackTrace(message, properties);
        }

        public void Metric(string metricName, double value, IDictionary<string, string> properties = null)
        {
            telemetryClient.TrackMetric(metricName, value, properties);
        }

        private static ExceptionTelemetry GetApplicationInsightsExceptionTelemetry(SeverityLevel severityLevel, Exception exception, string message, IDictionary<string, string> properties)
        {
            var exceptionTelemetry = new ExceptionTelemetry(exception)
            {
                SeverityLevel = severityLevel,
            };

            if (!message.IsNullOrEmpty())
            {
                exceptionTelemetry.Message = $"{message} --> {exception.Message}";
            }

            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    exceptionTelemetry.Properties.Add(prop);
                }
            }

            return exceptionTelemetry;
        }
    }
}
