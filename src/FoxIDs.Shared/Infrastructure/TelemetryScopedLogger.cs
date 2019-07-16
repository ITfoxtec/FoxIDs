using ITfoxtec.Identity;
using System;
using System.Collections.Generic;

namespace FoxIDs.Infrastructure
{
    public class TelemetryScopedLogger : IDisposable
    {
        private readonly TelemetryLogger telemetryLogger;
        private readonly TenantTrackLogger tenantTrackLogger;
        private readonly List<string> traceMessages = new List<string>();
        private readonly IDictionary<string, string> scopeProperties = new Dictionary<string, string>();

        public TelemetryScopedLogger(TelemetryLogger telemetryLogger, TenantTrackLogger tenantTrackLogger)
        {
            this.telemetryLogger = telemetryLogger;
            this.tenantTrackLogger = tenantTrackLogger;
        }

        public void Warning(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryLogger.Warning(exception, AddScopeProperty(properties), metrics);
            tenantTrackLogger.Warning(exception, AddScopeProperty(properties), metrics);
        }
        public void Warning(Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryLogger.Warning(exception, message, AddScopeProperty(properties), metrics);
            tenantTrackLogger.Warning(exception, message, AddScopeProperty(properties), metrics);
        }

        public void Error(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryLogger.Error(exception, AddScopeProperty(properties), metrics);
            tenantTrackLogger.Error(exception, AddScopeProperty(properties), metrics);
        }
        public void Error(Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryLogger.Error(exception, message, AddScopeProperty(properties), metrics);
            tenantTrackLogger.Error(exception, message, AddScopeProperty(properties), metrics);
        }

        public void CriticalError(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryLogger.CriticalError(exception, AddScopeProperty(properties), metrics);
            tenantTrackLogger.CriticalError(exception, AddScopeProperty(properties), metrics);
        }
        public void CriticalError(Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryLogger.CriticalError(exception, message, AddScopeProperty(properties), metrics);
            tenantTrackLogger.CriticalError(exception, message, AddScopeProperty(properties), metrics);
        }

        public void Event(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryLogger.Event(eventName, AddScopeProperty(properties), metrics);
            tenantTrackLogger.Event(eventName, AddScopeProperty(properties), metrics);
        }

        public void ScopeTrace(string message, IDictionary<string, string> properties = null, bool triggerEvent = false)
        {
            if (triggerEvent) Event(message, properties);
            traceMessages.Add(message);
            if (properties != null)
            {
                foreach(var prop in properties)
                {
                    SetScopeProperty(prop);
                }
            }
        }

        public void SetScopeProperty(string key, string value)
        {
            SetScopeProperty(new KeyValuePair<string, string>(key, value));
        }

        private void SetScopeProperty(KeyValuePair<string, string> prop)
        {
            scopeProperties[prop.Key] = prop.Value;
        }

        private IDictionary<string, string> AddScopeProperty(IDictionary<string, string> properties)
        {
            if (properties == null) properties = new Dictionary<string, string>();
            foreach(var prop in scopeProperties)
            {
                if(!properties.ContainsKey(prop.Key))
                {
                    properties.Add(prop);
                }
            }
            return properties;
        }

        bool isDisposed = false;
        public void Dispose()
        {
            if(!isDisposed)
            {
                isDisposed = true;
                if(traceMessages.Count > 0 || scopeProperties.Count > 0)
                {
                    telemetryLogger.Trace(traceMessages.ToJson(), scopeProperties);
                    tenantTrackLogger.Trace(traceMessages.ToJson(), scopeProperties);
                }
            }
        }
    }
}
