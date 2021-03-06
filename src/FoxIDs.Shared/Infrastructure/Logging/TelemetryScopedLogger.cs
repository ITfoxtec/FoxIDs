﻿using FoxIDs.Infrastructure.Logging;
using FoxIDs.Models;
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
        private readonly TelemetryScopedStreamLogger telemetryScopedStreamLogger;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly List<TraceMessage> traceMessages = new List<TraceMessage>();

        public TelemetryScopedLogger(TelemetryLogger telemetryLogger, TelemetryScopedProperties telemetryScopedProperties, TelemetryScopedStreamLogger telemetryScopedStreamLogger, IHttpContextAccessor httpContextAccessor)
        {
            this.telemetryLogger = telemetryLogger;
            this.telemetryScopedProperties = telemetryScopedProperties;
            this.telemetryScopedStreamLogger = telemetryScopedStreamLogger;
            this.httpContextAccessor = httpContextAccessor;
        }

        public void Warning(Exception exception, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            telemetryLogger.Warning(exception, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);

            if (RouteBinding?.Logging?.ScopedStreamLoggers?.Count > 0)
            {
                foreach (var scopedStreamLogger in RouteBinding?.Logging.ScopedStreamLoggers.Where(l => l.LogWarning))
                {
                    telemetryScopedStreamLogger.Warning(scopedStreamLogger, exception, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
                }
            }
        }
        public void Warning(Exception exception, string message, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            telemetryLogger.Warning(exception, message, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);

            if (RouteBinding?.Logging?.ScopedStreamLoggers?.Count > 0)
            {
                foreach (var scopedStreamLogger in RouteBinding?.Logging.ScopedStreamLoggers.Where(l => l.LogWarning))
                {
                    telemetryScopedStreamLogger.Warning(scopedStreamLogger, exception, message, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
                }
            }
        }

        public void Error(Exception exception, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            telemetryLogger.Error(exception, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);

            if (RouteBinding?.Logging?.ScopedStreamLoggers?.Count > 0)
            {
                foreach (var scopedStreamLogger in RouteBinding?.Logging.ScopedStreamLoggers.Where(l => l.LogError))
                {
                    telemetryScopedStreamLogger.Error(scopedStreamLogger, exception, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
                }
            }
        }
        public void Error(Exception exception, string message, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            telemetryLogger.Error(exception, message, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);

            if (RouteBinding?.Logging?.ScopedStreamLoggers?.Count > 0)
            {
                foreach (var scopedStreamLogger in RouteBinding?.Logging.ScopedStreamLoggers.Where(l => l.LogError))
                {
                    telemetryScopedStreamLogger.Error(scopedStreamLogger, exception, message, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
                }
            }
        }

        public void CriticalError(Exception exception, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            telemetryLogger.CriticalError(exception, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);

            if (RouteBinding?.Logging?.ScopedStreamLoggers?.Count > 0)
            {
                foreach (var scopedStreamLogger in RouteBinding?.Logging.ScopedStreamLoggers.Where(l => l.LogCriticalError))
                {
                    telemetryScopedStreamLogger.CriticalError(scopedStreamLogger, exception, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
                }
            }
        }
        public void CriticalError(Exception exception, string message, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            telemetryLogger.CriticalError(exception, message, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);

            if (RouteBinding?.Logging?.ScopedStreamLoggers?.Count > 0)
            {
                foreach (var scopedStreamLogger in RouteBinding?.Logging.ScopedStreamLoggers.Where(l => l.LogCriticalError))
                {
                    telemetryScopedStreamLogger.CriticalError(scopedStreamLogger, exception, message, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
                }
            }
        }

        public void Event(string eventName, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            telemetryLogger.Event(eventName, properties, metrics);

            if (RouteBinding?.Logging?.ScopedStreamLoggers?.Count > 0)
            {
                foreach (var scopedStreamLogger in RouteBinding?.Logging.ScopedStreamLoggers.Where(l => l.LogEvent))
                {
                    telemetryScopedStreamLogger.Event(scopedStreamLogger, eventName, telemetryScopedProperties.Properties.ConcatOnce(properties), metrics);
                }
            }                        
        }

        public void ScopeTrace(Func<string> message, IDictionary<string, string> scopeProperties = null, bool triggerEvent = false, TraceTypes traceType = TraceTypes.Info)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);

            var save = RouteBinding?.Logging != null && traceType switch
            {
                TraceTypes.Info => RouteBinding?.Logging.ScopedLogger?.LogInfoTrace == true || RouteBinding?.Logging.ScopedStreamLoggers?.Where(l => l.LogInfoTrace).Any() == true,
                TraceTypes.Claim => RouteBinding?.Logging.ScopedLogger?.LogClaimTrace == true || RouteBinding?.Logging.ScopedStreamLoggers?.Where(l => l.LogClaimTrace).Any() == true,
                TraceTypes.Message => RouteBinding?.Logging.ScopedLogger?.LogMessageTrace == true || RouteBinding?.Logging.ScopedStreamLoggers?.Where(l => l.LogMessageTrace).Any() == true,
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

            var saveScoped = RouteBinding?.Logging?.ScopedLogger?.LogMetric == true;
            var saveScopedStream = RouteBinding?.Logging?.ScopedStreamLoggers?.Where(l => l.LogMetric).Any() == true;

            if (saveScoped || saveScopedStream) 
            {
                var messageData = new MetricMessage();
                metric(messageData);

                if (saveScoped)
                {
                    telemetryLogger.Metric(messageData.Message, messageData.Value, properties);
                }

                if (RouteBinding?.Logging?.ScopedStreamLoggers?.Count > 0)
                {
                    foreach (var scopedStreamLogger in RouteBinding.Logging.ScopedStreamLoggers.Where(l => l.LogMetric))
                    {
                        telemetryScopedStreamLogger.Metric(scopedStreamLogger, messageData.Message, messageData.Value, telemetryScopedProperties.Properties.ConcatOnce(properties));
                    }
                }
            }
        }

        public void SetScopeProperty(string key, string value)
        {
            telemetryScopedProperties.SetScopeProperty(new KeyValuePair<string, string>(key, value));
        }

        private RouteBinding RouteBinding => httpContextAccessor?.HttpContext?.GetRouteBinding();

        bool isDisposed = false;
        public void Dispose()
        {
            if(!isDisposed)
            {
                isDisposed = true;
                if (RouteBinding?.Logging != null && traceMessages.Count > 0)
                {
                    if (RouteBinding.Logging.ScopedLogger != null)
                    {
                        var scopedLogger = RouteBinding.Logging.ScopedLogger;
                        var telemetryLoggertraceMessages = traceMessages.Where(m =>
                            (m.TraceType == TraceTypes.Info && scopedLogger.LogInfoTrace) ||
                            (m.TraceType == TraceTypes.Claim && scopedLogger.LogClaimTrace) ||
                            (m.TraceType == TraceTypes.Message && scopedLogger.LogMessageTrace));
                        if (telemetryLoggertraceMessages.Count() > 0)
                        {
                            telemetryLogger.Trace(telemetryLoggertraceMessages.ToJson(), telemetryScopedProperties.Properties.ConcatOnce(new Dictionary<string, string> { { Constants.Logs.Type, nameof(TelemetryScopedLogger.ScopeTrace) } }));
                        }
                    }

                    if (RouteBinding.Logging.ScopedStreamLoggers?.Count() > 0)
                    {
                        foreach (var scopedStreamLogger in RouteBinding.Logging.ScopedStreamLoggers)
                        {
                            var scopedStreamLoggertraceMessages = traceMessages.Where(m =>
                                (m.TraceType == TraceTypes.Info && scopedStreamLogger.LogInfoTrace) ||
                                (m.TraceType == TraceTypes.Claim && scopedStreamLogger.LogClaimTrace) ||
                                (m.TraceType == TraceTypes.Message && scopedStreamLogger.LogMessageTrace));
                            if (scopedStreamLoggertraceMessages.Count() > 0)
                            {
                                telemetryScopedStreamLogger.Trace(scopedStreamLogger, scopedStreamLoggertraceMessages.ToJson(), telemetryScopedProperties.Properties);
                            }
                        }
                    }
                }
            }
        }
    }
}
