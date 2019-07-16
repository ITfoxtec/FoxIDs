using ITfoxtec.Identity;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;

namespace FoxIDs.Infrastructure
{
    public class TelemetryLogger
    {
        private readonly TelemetryClient telemetryClient;

        public TelemetryLogger(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
        }

        public void Warning(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            Warning(exception, null, properties, metrics);
        }
        public void Warning(Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryClient.TrackException(exception, AddErrorLevelInfo(properties, "warning", message), metrics);
        }

        public void Error(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            Error(exception, null, properties, metrics);
        }
        public void Error(Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryClient.TrackException(exception, AddErrorLevelInfo(properties, "error", message), metrics);
        }

        public void CriticalError(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            CriticalError(exception, null, properties, metrics);
        }
        public void CriticalError(Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryClient.TrackException(exception, AddErrorLevelInfo(properties, "criticalError", message), metrics);
        }

        public void Event(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryClient.TrackEvent(eventName, properties, metrics);
        }
        public void Trace(string message, IDictionary<string, string> properties = null)
        {
            telemetryClient.TrackTrace(message, properties);
        }

        private static IDictionary<string, string> AddErrorLevelInfo(IDictionary<string, string> properties, string level, string message)
        {
            if (properties == null) properties = new Dictionary<string, string>();
            properties.Add("level", level);
            if(!message.IsNullOrEmpty())
                properties.Add("message", message);
            return properties;
        }
    }
}
