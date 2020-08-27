using ITfoxtec.Identity;
using System;
using System.Collections.Generic;

namespace FoxIDs.Infrastructure
{
    public class TelemetryScopedLogger : IDisposable
    {
        private readonly TelemetryLogger telemetryLogger;
        private readonly TelemetryScopedProperties telemetryScopedProperties;
        private readonly TenantTrackLogger tenantTrackLogger;
        private readonly List<string> traceMessages = new List<string>();

        public TelemetryScopedLogger(TelemetryLogger telemetryLogger, TelemetryScopedProperties telemetryScopedProperties, TenantTrackLogger tenantTrackLogger)
        {
            this.telemetryLogger = telemetryLogger;
            this.telemetryScopedProperties = telemetryScopedProperties;
            this.tenantTrackLogger = tenantTrackLogger;
        }

        public void Warning(Exception exception, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            telemetryLogger.Warning(exception, properties, metrics);
            tenantTrackLogger.Warning(exception, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
        }
        public void Warning(Exception exception, string message, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            telemetryLogger.Warning(exception, message, properties, metrics);
            tenantTrackLogger.Warning(exception, message, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
        }

        public void Error(Exception exception, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            telemetryLogger.Error(exception, properties, metrics);
            tenantTrackLogger.Error(exception, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
        }
        public void Error(Exception exception, string message, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            telemetryLogger.Error(exception, message, properties, metrics);
            tenantTrackLogger.Error(exception, message, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
        }

        public void CriticalError(Exception exception, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            telemetryLogger.CriticalError(exception, properties, metrics);
            tenantTrackLogger.CriticalError(exception, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
        }
        public void CriticalError(Exception exception, string message, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            telemetryLogger.CriticalError(exception, message, properties, metrics);
            tenantTrackLogger.CriticalError(exception, message, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
        }

        public void Event(string eventName, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            telemetryLogger.Event(eventName, properties, metrics);
            tenantTrackLogger.Event(eventName, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
        }

        public void ScopeTrace(string message, IDictionary<string, string> scopeProperties = null, bool triggerEvent = false)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            if (triggerEvent) Event(message);
            traceMessages.Add(message);
        }

        public void ScopeMetric(string message, double value, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            telemetryLogger.Metric(message, value, properties);
            tenantTrackLogger.Metric(message, value, telemetryScopedProperties.Properties.ConcatOnce(properties));
        }

        public void SetScopeProperty(string key, string value)
        {
            telemetryScopedProperties.SetScopeProperty(new KeyValuePair<string, string>(key, value));
        }

        bool isDisposed = false;
        public void Dispose()
        {
            if(!isDisposed)
            {
                isDisposed = true;
                if(traceMessages.Count > 0 )
                {
                    telemetryLogger.Trace(traceMessages.ToJson(), telemetryScopedProperties.Properties.ConcatOnce(new Dictionary<string, string> { { "type", nameof(TelemetryScopedLogger.ScopeTrace) } } ));
                    tenantTrackLogger.Trace(traceMessages.ToJson(), telemetryScopedProperties.Properties);
                }
            }
        }
    }
}
