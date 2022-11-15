using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace FoxIDs.Infrastructure
{
    public class TelemetryLogger
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IHttpContextAccessor httpContextAccessor;

        public TelemetryLogger(TelemetryClient telemetryClient, IHttpContextAccessor httpContextAccessor = null)
        {
            this.telemetryClient = telemetryClient;
            this.httpContextAccessor = httpContextAccessor;
        }

        public void Warning(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            Warning(exception, null, properties, metrics);
        }
        public void Warning(Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            GetTelemetryClient().TrackException(GetExceptionTelemetry(SeverityLevel.Warning, exception, message, properties, metrics));
        }

        public void Error(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            Error(exception, null, properties, metrics);
        }
        public void Error(Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            GetTelemetryClient().TrackException(GetExceptionTelemetry(SeverityLevel.Error, exception, message, properties, metrics));
        }

        public void CriticalError(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            CriticalError(exception, null, properties, metrics);
        }
        public void CriticalError(Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            GetTelemetryClient().TrackException(GetExceptionTelemetry(SeverityLevel.Critical, exception, message, properties, metrics));
        }

        public void Event(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            GetTelemetryClient().TrackEvent(eventName, properties, metrics);
        }
        public void Trace(string message, IDictionary<string, string> properties = null)
        {
            GetTelemetryClient().TrackTrace(message, SeverityLevel.Verbose, properties);
        }

        public void Metric(string message, double value, IDictionary<string, string> properties = null)
        {
            GetTelemetryClient().TrackMetric(message, value, properties);
        }

        private TelemetryClient GetTelemetryClient()
        {
            if (httpContextAccessor != null && RouteBinding?.TelemetryClient != null)
            {
                return RouteBinding.TelemetryClient;
            }
            else
            {
                return telemetryClient;
            }
        }

        private RouteBinding RouteBinding => httpContextAccessor?.HttpContext?.GetRouteBinding();

        private static ExceptionTelemetry GetExceptionTelemetry(SeverityLevel severityLevel, Exception exception, string message, IDictionary<string, string> properties, IDictionary<string, double> metrics)
        {
            var exceptionTelemetry = new ExceptionTelemetry(exception)
            {
                SeverityLevel = severityLevel,
            };

            exceptionTelemetry.Properties.Add("handled", true.ToString());

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
