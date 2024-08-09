using FoxIDs.Infrastructure.Logging;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace FoxIDs.Infrastructure
{
    public class TelemetryLogger
    {
        private readonly Settings settings;
        private readonly IServiceProvider serviceProvider;

        public TelemetryClient ApplicationInsightsTelemetryClient { private get; set; }

        public TelemetryLogger(Settings settings, IServiceProvider serviceProvider)
        {
            this.settings = settings;
            this.serviceProvider = serviceProvider;
        }

        public void Warning(Exception exception, IDictionary<string, string> properties = null)
        {
            Warning(exception, null, properties);
        }
        public void Warning(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            var exceptionTelemetry = GetApplicationInsightsExceptionTelemetry(SeverityLevel.Warning, exception, message, properties);
            if (settings.Options.Log == LogOptions.Stdout || settings.Options.Log == LogOptions.OpenSearchAndStdoutErrors || IsDevelopment())
            {
                GetStdoutTelemetryLogger().LogWarning(exceptionTelemetry);
            }

            if(settings.Options.Log == LogOptions.OpenSearchAndStdoutErrors)
            {

            }
            else if (settings.Options.Log == LogOptions.ApplicationInsights)
            {
                GetApplicationInsightsTelemetryClient().TrackException(exceptionTelemetry);
            }
        }


        public void Error(Exception exception, IDictionary<string, string> properties = null)
        {
            Error(exception, null, properties);
        }
        public void Error(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            var exceptionTelemetry = GetApplicationInsightsExceptionTelemetry(SeverityLevel.Error, exception, message, properties);
            if (settings.Options.Log == LogOptions.Stdout || settings.Options.Log == LogOptions.OpenSearchAndStdoutErrors || IsDevelopment())
            {
                GetStdoutTelemetryLogger().LogError(exceptionTelemetry);
            }

            if (settings.Options.Log == LogOptions.OpenSearchAndStdoutErrors)
            {

            }
            else if (settings.Options.Log == LogOptions.ApplicationInsights)
            {
                GetApplicationInsightsTelemetryClient().TrackException(exceptionTelemetry);
            }
        }

        public void CriticalError(Exception exception, IDictionary<string, string> properties = null)
        {
            CriticalError(exception, null, properties);
        }
        public void CriticalError(Exception exception, string message, IDictionary<string, string> properties = null)
        {
            var exceptionTelemetry = GetApplicationInsightsExceptionTelemetry(SeverityLevel.Critical, exception, message, properties);
            if (settings.Options.Log == LogOptions.Stdout || settings.Options.Log == LogOptions.OpenSearchAndStdoutErrors || IsDevelopment())
            {
                GetStdoutTelemetryLogger().LogCritical(exceptionTelemetry);
            }

            if (settings.Options.Log == LogOptions.OpenSearchAndStdoutErrors)
            {

            }
            else if (settings.Options.Log == LogOptions.ApplicationInsights)
            {
                GetApplicationInsightsTelemetryClient().TrackException(exceptionTelemetry);
            }
        }

        public void Event(string eventName, IDictionary<string, string> properties = null)
        {
            var eventTelemetry = GetApplicationInsightsEventTelemetry(eventName, properties);
            if (settings.Options.Log == LogOptions.Stdout || settings.Options.Log == LogOptions.OpenSearchAndStdoutErrors || IsDevelopment())
            {
                GetStdoutTelemetryLogger().LogEvent(eventTelemetry);
            }

            if (settings.Options.Log == LogOptions.OpenSearchAndStdoutErrors)
            {

            }
            else if (settings.Options.Log == LogOptions.ApplicationInsights)
            {
                GetApplicationInsightsTelemetryClient().TrackEvent(eventTelemetry);
            }
        }

        public void Trace(string message, IDictionary<string, string> properties = null)
        {
            var traceTelemetry = GetApplicationInsightsTraceTelemetry(message, properties);
            if (settings.Options.Log == LogOptions.Stdout || settings.Options.Log == LogOptions.OpenSearchAndStdoutErrors || IsDevelopment())
            {
                GetStdoutTelemetryLogger().LogTrace(traceTelemetry);
            }

            if (settings.Options.Log == LogOptions.OpenSearchAndStdoutErrors)
            {

            }
            else if (settings.Options.Log == LogOptions.ApplicationInsights)
            {
                GetApplicationInsightsTelemetryClient().TrackTrace(traceTelemetry);
            }
        }

        public void Metric(string metricName, double value, IDictionary<string, string> properties = null)
        {
            var metricTelemetry = GetApplicationInsightsMetricTelemetry(metricName, value, properties);
            if (settings.Options.Log == LogOptions.Stdout || settings.Options.Log == LogOptions.OpenSearchAndStdoutErrors || IsDevelopment())
            {
                GetStdoutTelemetryLogger().LogMetric(metricTelemetry);
            }

            if (settings.Options.Log == LogOptions.OpenSearchAndStdoutErrors)
            {

            }
            else if (settings.Options.Log == LogOptions.ApplicationInsights)
            {
                GetApplicationInsightsTelemetryClient().TrackMetric(metricTelemetry);
            }
        }


        private bool IsDevelopment()
        {
            var environment = serviceProvider.GetService<IWebHostEnvironment>();
            if (environment != null)
            {
                return environment.IsDevelopment();
            }
            else
            {
                return false;
            }
        }

        private StdoutTelemetryLogger GetStdoutTelemetryLogger()
        {
            return serviceProvider.GetService<StdoutTelemetryLogger>();
        }

        private TelemetryClient GetApplicationInsightsTelemetryClient()
        {
            if(ApplicationInsightsTelemetryClient != null)
            {
                return ApplicationInsightsTelemetryClient;
            }
            else
            {
                return serviceProvider.GetService<TelemetryClient>();
            }
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

        private static EventTelemetry GetApplicationInsightsEventTelemetry(string eventName, IDictionary<string, string> properties)
        {
            var eventTelemetry = new EventTelemetry(eventName);

            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    eventTelemetry.Properties.Add(prop);
                }
            }

            return eventTelemetry;
        }

        private static TraceTelemetry GetApplicationInsightsTraceTelemetry(string message, IDictionary<string, string> properties)
        {
            var traceTelemetry = new TraceTelemetry(message);

            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    traceTelemetry.Properties.Add(prop);
                }
            }

            return traceTelemetry;
        }
      
        private static MetricTelemetry GetApplicationInsightsMetricTelemetry(string metricName, double value, IDictionary<string, string> properties)
        {
            var metricTelemetry = new MetricTelemetry() { Name = metricName, Sum = value };

            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    metricTelemetry.Properties.Add(prop);
                }
            }

            return metricTelemetry;
        }
    }
}
