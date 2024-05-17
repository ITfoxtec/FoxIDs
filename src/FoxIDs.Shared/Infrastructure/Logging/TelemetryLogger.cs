using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading;

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
            (var telemetryClient, var flush) = GetTelemetryClient();
            telemetryClient.TrackException(GetExceptionTelemetry(SeverityLevel.Warning, exception, message, properties, metrics));
            if(flush)
            {
                telemetryClient.Flush();
                Thread.Sleep(5000);
            }
        }

        public void Error(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            Error(exception, null, properties, metrics);
        }
        public void Error(Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            (var telemetryClient, var flush) = GetTelemetryClient();
            telemetryClient.TrackException(GetExceptionTelemetry(SeverityLevel.Error, exception, message, properties, metrics));
            if (flush)
            {
                telemetryClient.Flush();
                Thread.Sleep(5000);
            }
        }

        public void CriticalError(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            CriticalError(exception, null, properties, metrics);
        }
        public void CriticalError(Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            (var telemetryClient, var flush) = GetTelemetryClient();
            telemetryClient.TrackException(GetExceptionTelemetry(SeverityLevel.Critical, exception, message, properties, metrics));
            if (flush)
            {
                telemetryClient.Flush();
                Thread.Sleep(5000);
            }
        }

        public void Event(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            (var telemetryClient, var flush) = GetTelemetryClient();
            telemetryClient.TrackEvent(eventName, properties, metrics);
            if (flush)
            {
                telemetryClient.Flush();
                Thread.Sleep(5000);
            }
        }
        public void Trace(string message, IDictionary<string, string> properties = null)
        {
            (var telemetryClient, var flush) = GetTelemetryClient();
            telemetryClient.TrackTrace(message, properties);
            if (flush)
            {
                telemetryClient.Flush();
                Thread.Sleep(5000);
            }
        }

        public void Metric(string message, double value, IDictionary<string, string> properties = null)
        {
            (var telemetryClient, var flush) = GetTelemetryClient();
            telemetryClient.TrackMetric(message, value, properties);
            if (flush)
            {
                telemetryClient.Flush();
                Thread.Sleep(5000);
            }
        }

        private (TelemetryClient, bool flush) GetTelemetryClient()
        {
            if (httpContextAccessor != null && RouteBinding?.TelemetryClient != null)
            {
                return (RouteBinding.TelemetryClient, false);
            }
            else
            {
                return (telemetryClient, true);
            }
        }

        private RouteBinding RouteBinding => httpContextAccessor?.HttpContext?.GetRouteBinding();

        private static ExceptionTelemetry GetExceptionTelemetry(SeverityLevel severityLevel, Exception exception, string message, IDictionary<string, string> properties, IDictionary<string, double> metrics)
        {
            var exceptionTelemetry = new ExceptionTelemetry(exception)
            {
                SeverityLevel = severityLevel,
            };

            exceptionTelemetry.Properties.Add(Constants.Logs.LoggingHandledKey, true.ToString());

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
