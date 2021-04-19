using FoxIDs.Infrastructure.Logging;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Infrastructure
{
    public class TelemetryScopedLogger : IDisposable
    {
        private readonly TelemetryLogger telemetryLogger;
        private readonly TelemetryScopedProperties telemetryScopedProperties;
        private readonly TenantTrackLogger tenantTrackLogger;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly List<TraceMessage> traceMessages = new List<TraceMessage>();

        public TelemetryScopedLogger(TelemetryLogger telemetryLogger, TelemetryScopedProperties telemetryScopedProperties, TenantTrackLogger tenantTrackLogger, IHttpContextAccessor httpContextAccessor)
        {
            this.telemetryLogger = telemetryLogger;
            this.telemetryScopedProperties = telemetryScopedProperties;
            this.tenantTrackLogger = tenantTrackLogger;
            this.httpContextAccessor = httpContextAccessor;
        }

        public void Warning(Exception exception, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);

            var routeBinding = httpContextAccessor.HttpContext.GetRouteBinding();
            var saveScoped = routeBinding.Logging == null || routeBinding.Logging.ScopedLogger?.LogWarning == true;
            if (saveScoped)
            {
                telemetryLogger.Warning(exception, properties, metrics);
            }

            if (routeBinding.Logging.TrackLoggers?.Count > 0)
            {
                foreach (var trackLogger in routeBinding.Logging.TrackLoggers.Where(l => l.LogWarning))
                {
                    tenantTrackLogger.Warning(trackLogger, exception, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
                }
            }
        }
        public void Warning(Exception exception, string message, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);

            var routeBinding = httpContextAccessor.HttpContext.GetRouteBinding();
            var saveScoped = routeBinding.Logging == null || routeBinding.Logging.ScopedLogger?.LogWarning == true;
            if (saveScoped)
            {
                telemetryLogger.Warning(exception, message, properties, metrics);
            }

            if (routeBinding.Logging.TrackLoggers?.Count > 0)
            {
                foreach (var trackLogger in routeBinding.Logging.TrackLoggers.Where(l => l.LogWarning))
                {
                    tenantTrackLogger.Warning(trackLogger, exception, message, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
                }
            }
        }

        public void Error(Exception exception, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);

            var routeBinding = httpContextAccessor.HttpContext.GetRouteBinding();
            var saveScoped = routeBinding.Logging == null || routeBinding.Logging.ScopedLogger?.LogError == true;
            if (saveScoped)
            {
                telemetryLogger.Error(exception, properties, metrics);
            }

            if (routeBinding.Logging.TrackLoggers?.Count > 0)
            {
                foreach (var trackLogger in routeBinding.Logging.TrackLoggers.Where(l => l.LogError))
                {
                    tenantTrackLogger.Error(trackLogger, exception, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
                }
            }
        }
        public void Error(Exception exception, string message, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);

            var routeBinding = httpContextAccessor.HttpContext.GetRouteBinding();
            var saveScoped = routeBinding.Logging == null || routeBinding.Logging.ScopedLogger?.LogError == true;
            if (saveScoped)
            {
                telemetryLogger.Error(exception, message, properties, metrics);
            }

            if (routeBinding.Logging.TrackLoggers?.Count > 0)
            {
                foreach (var trackLogger in routeBinding.Logging.TrackLoggers.Where(l => l.LogError))
                {
                    tenantTrackLogger.Error(trackLogger, exception, message, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
                }
            }
        }

        public void CriticalError(Exception exception, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);

            var routeBinding = httpContextAccessor.HttpContext.GetRouteBinding();
            var saveScoped = routeBinding.Logging == null || routeBinding.Logging.ScopedLogger?.LogCriticalError == true;
            if (saveScoped)
            {
                telemetryLogger.CriticalError(exception, properties, metrics);
            }

            if (routeBinding.Logging.TrackLoggers?.Count > 0)
            {
                foreach (var trackLogger in routeBinding.Logging.TrackLoggers.Where(l => l.LogCriticalError))
                {
                    tenantTrackLogger.CriticalError(trackLogger, exception, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
                }
            }
        }
        public void CriticalError(Exception exception, string message, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);

            var routeBinding = httpContextAccessor.HttpContext.GetRouteBinding();
            var saveScoped = routeBinding.Logging == null || routeBinding.Logging.ScopedLogger?.LogCriticalError == true;
            if (saveScoped)
            {
                telemetryLogger.CriticalError(exception, message, properties, metrics);
            }

            if (routeBinding.Logging.TrackLoggers?.Count > 0)
            {
                foreach (var trackLogger in routeBinding.Logging.TrackLoggers.Where(l => l.LogCriticalError))
                {
                    tenantTrackLogger.CriticalError(trackLogger, exception, message, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
                }
            }
        }

        public void Event(string eventName, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);

            var routeBinding = httpContextAccessor.HttpContext.GetRouteBinding();
            var saveScoped = routeBinding.Logging == null || routeBinding.Logging.ScopedLogger?.LogEvent == true;
            if (saveScoped)
            {
                telemetryLogger.Event(eventName, properties, metrics);
            }

            if (routeBinding.Logging.TrackLoggers?.Count > 0)
            {
                foreach (var trackLogger in routeBinding.Logging.TrackLoggers.Where(l => l.LogEvent))
                {
                    tenantTrackLogger.Event(trackLogger, eventName, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
                }
            }                        
        }

        public void ScopeTrace(Func<string> message, IDictionary<string, string> scopeProperties = null, bool triggerEvent = false, TraceTypes traceType = TraceTypes.Info)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);

            var routeBinding = httpContextAccessor.HttpContext.GetRouteBinding();
            var save = routeBinding.Logging != null && traceType switch
            {
                TraceTypes.Info => routeBinding.Logging.ScopedLogger?.LogInfoTrace == true || routeBinding.Logging.TrackLoggers?.Where(l => l.LogInfoTrace).Any() == true,
                TraceTypes.Claim => routeBinding.Logging.ScopedLogger?.LogClaimTrace == true || routeBinding.Logging.TrackLoggers?.Where(l => l.LogClaimTrace).Any() == true,
                TraceTypes.Message => routeBinding.Logging.ScopedLogger?.LogMessageTrace == true || routeBinding.Logging.TrackLoggers?.Where(l => l.LogMessageTrace).Any() == true,
                _ => throw new NotSupportedException($"Trace type '{traceType}' not supported.")
            };

            var messageString = save || traceType == TraceTypes.Info && triggerEvent ? message() : null;
            if (messageString != null)
            {
                if (traceType == TraceTypes.Info && triggerEvent) Event(messageString);
                if (save) traceMessages.Add(new TraceMessage { TraceType = traceType, Message = messageString });
            }
        }

        public void ScopeMetric(Action<MetricMessage> metric, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);

            var routeBinding = httpContextAccessor.HttpContext.GetRouteBinding();
            var saveScoped = routeBinding.Logging != null && routeBinding.Logging.ScopedLogger?.LogMetric == true;
            var saveTrack = routeBinding.Logging != null && routeBinding.Logging.TrackLoggers?.Where(l => l.LogMetric).Any() == true;

            if (saveScoped || saveTrack) 
            {
                var messageData = new MetricMessage();
                metric(messageData);

                if (saveScoped)
                {
                    telemetryLogger.Metric(messageData.Message, messageData.Value, properties);
                }

                if (routeBinding.Logging.TrackLoggers?.Count > 0)
                {
                    foreach (var trackLogger in routeBinding.Logging.TrackLoggers.Where(l => l.LogMetric))
                    {
                        tenantTrackLogger.Metric(trackLogger, messageData.Message, messageData.Value, telemetryScopedProperties.Properties.ConcatOnce(properties));
                    }
                }
            }
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
                var routeBinding = httpContextAccessor.HttpContext.GetRouteBinding();
                if (routeBinding.Logging != null && traceMessages.Count > 0 )
                {
                    if (routeBinding.Logging.ScopedLogger != null)
                    {
                        var scopedLogger = routeBinding.Logging.ScopedLogger;
                        var telemetryLoggertraceMessages = traceMessages.Where(m => 
                            (m.TraceType == TraceTypes.Info && scopedLogger.LogInfoTrace) || 
                            (m.TraceType == TraceTypes.Claim && scopedLogger.LogClaimTrace) || 
                            (m.TraceType == TraceTypes.Message && scopedLogger.LogMessageTrace));
                        telemetryLogger.Trace(telemetryLoggertraceMessages.ToJson(), telemetryScopedProperties.Properties.ConcatOnce(new Dictionary<string, string> { { "type", nameof(TelemetryScopedLogger.ScopeTrace) } }));
                    }

                    if (routeBinding.Logging.TrackLoggers?.Count() > 0)
                    {
                        foreach (var trackLogger in routeBinding.Logging.TrackLoggers)
                        {
                            var trackLoggertraceMessages = traceMessages.Where(m =>
                                (m.TraceType == TraceTypes.Info && trackLogger.LogInfoTrace) ||
                                (m.TraceType == TraceTypes.Claim && trackLogger.LogClaimTrace) ||
                                (m.TraceType == TraceTypes.Message && trackLogger.LogMessageTrace));
                            tenantTrackLogger.Trace(trackLogger, traceMessages.ToJson(), telemetryScopedProperties.Properties);
                        }
                    }
                }
            }
        }
    }
}
