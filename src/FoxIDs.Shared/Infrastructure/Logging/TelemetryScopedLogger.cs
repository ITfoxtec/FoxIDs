using FoxIDs.Infrastructure.Logging;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace FoxIDs.Infrastructure
{
    public class TelemetryScopedLogger : IDisposable
    {
        private TelemetryLogger telemetryLogger;
        private readonly TelemetryScopedProperties telemetryScopedProperties;
        private readonly TelemetryScopedStreamLogger telemetryScopedStreamLogger;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly List<TraceMessage> traceMessages = new List<TraceMessage>();
        private Models.Logging logging;

        public TelemetryScopedLogger(TelemetryLogger telemetryLogger, TelemetryScopedProperties telemetryScopedProperties, TelemetryScopedStreamLogger telemetryScopedStreamLogger, IHttpContextAccessor httpContextAccessor)
        {
            this.telemetryLogger = telemetryLogger;
            this.telemetryScopedProperties = telemetryScopedProperties;
            this.telemetryScopedStreamLogger = telemetryScopedStreamLogger;
            this.httpContextAccessor = httpContextAccessor;       
        }

        public Models.Logging Logging
        {
            get 
            {
                if (logging != null)
                {
                    return logging;
                }
                else
                {
                    return httpContextAccessor?.HttpContext?.GetRouteBinding()?.Logging;
                }
            }
            set
            {
                logging = value;
            }
        }

        public TelemetryLogger TelemetryLogger
        {
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(TelemetryLogger));
                }
                telemetryLogger = value;
            }
        }

        public void Warning(Exception exception, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null, bool logToScopeStream = true)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            var allProperties = ConcatOnceIfProperties(properties);
            telemetryLogger.Warning(exception, allProperties, metrics);

            if (logToScopeStream && Logging?.ScopedStreamLoggers?.Count > 0)
            {
                foreach (var scopedStreamLogger in Logging.ScopedStreamLoggers.Where(l => l.LogWarning))
                {
                    telemetryScopedStreamLogger.Warning(scopedStreamLogger, exception, allProperties, metrics);
                }
            }
        }

        public void Warning(Exception exception, string message, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null, bool logToScopeStream = true)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            var allProperties = ConcatOnceIfProperties(properties);
            telemetryLogger.Warning(exception, message, allProperties, metrics);

            if (logToScopeStream && Logging?.ScopedStreamLoggers?.Count > 0)
            {
                foreach (var scopedStreamLogger in Logging.ScopedStreamLoggers.Where(l => l.LogWarning))
                {
                    telemetryScopedStreamLogger.Warning(scopedStreamLogger, exception, message, allProperties, metrics);
                }
            }
        }

        public void Error(Exception exception, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null, bool logToScopeStream = true)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            var allProperties = ConcatOnceIfProperties(properties);
            telemetryLogger.Error(exception, allProperties, metrics);

            if (logToScopeStream && Logging?.ScopedStreamLoggers?.Count > 0)
            {
                foreach (var scopedStreamLogger in Logging.ScopedStreamLoggers.Where(l => l.LogError))
                {
                    telemetryScopedStreamLogger.Error(scopedStreamLogger, exception, allProperties, metrics);
                }
            }
        }
        public void Error(Exception exception, string message, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null, bool logToScopeStream = true)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            var allProperties = ConcatOnceIfProperties(properties);
            telemetryLogger.Error(exception, message, allProperties, metrics);

            if (logToScopeStream && Logging?.ScopedStreamLoggers?.Count > 0)
            {
                foreach (var scopedStreamLogger in Logging.ScopedStreamLoggers.Where(l => l.LogError))
                {
                    telemetryScopedStreamLogger.Error(scopedStreamLogger, exception, message, allProperties, metrics);
                }
            }
        }

        public void CriticalError(Exception exception, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null, bool logToScopeStream = true)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            var allProperties = ConcatOnceIfProperties(properties);
            telemetryLogger.CriticalError(exception, allProperties, metrics);

            if (logToScopeStream && Logging?.ScopedStreamLoggers?.Count > 0)
            {
                foreach (var scopedStreamLogger in Logging.ScopedStreamLoggers.Where(l => l.LogCriticalError))
                {
                    telemetryScopedStreamLogger.CriticalError(scopedStreamLogger, exception, allProperties, metrics);
                }
            }
        }
        public void CriticalError(Exception exception, string message, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null, bool logToScopeStream = true)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            var allProperties = ConcatOnceIfProperties(properties);
            telemetryLogger.CriticalError(exception, message, allProperties, metrics);

            if (logToScopeStream && Logging?.ScopedStreamLoggers?.Count > 0)
            {
                foreach (var scopedStreamLogger in Logging.ScopedStreamLoggers.Where(l => l.LogCriticalError))
                {
                    telemetryScopedStreamLogger.CriticalError(scopedStreamLogger, exception, message, allProperties, metrics);
                }
            }
        }

        public void Event(string eventName, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null, bool logToScopeStream = true)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);
            var allProperties = ConcatOnceIfProperties(properties);
            telemetryLogger.Event(eventName, allProperties, metrics);

            if (logToScopeStream && Logging?.ScopedStreamLoggers?.Count > 0)
            {
                foreach (var scopedStreamLogger in Logging.ScopedStreamLoggers.Where(l => l.LogEvent))
                {
                    telemetryScopedStreamLogger.Event(scopedStreamLogger, eventName, allProperties, metrics);
                }
            }                        
        }

        public void ScopeTrace(Func<string> message, IDictionary<string, string> scopeProperties = null, bool triggerEvent = false, TraceTypes traceType = TraceTypes.Info)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);

            var save = Logging != null && traceType switch
            {
                TraceTypes.Info => Logging.ScopedLogger?.LogInfoTrace == true || Logging.ScopedStreamLoggers?.Where(l => l.LogInfoTrace).Any() == true,
                TraceTypes.Claim => Logging.ScopedLogger?.LogClaimTrace == true || Logging.ScopedStreamLoggers?.Where(l => l.LogClaimTrace).Any() == true,
                TraceTypes.Message => Logging.ScopedLogger?.LogMessageTrace == true || Logging.ScopedStreamLoggers?.Where(l => l.LogMessageTrace).Any() == true,
                _ => throw new NotSupportedException($"Trace type '{traceType}' not supported.")
            };

            var messageString = save || traceType == TraceTypes.Info && triggerEvent ? message() : null;
            if (messageString != null)
            {
                if (traceType == TraceTypes.Info && triggerEvent) Event(messageString);
                if (save) traceMessages.Add(new TraceMessage { TraceType = traceType, Message = messageString });
            }
        }

        public void ScopeMetric(Action<MetricMessage> metric, IDictionary<string, string> scopeProperties = null, IDictionary<string, string> properties = null, bool logToScopeStream = true)
        {
            telemetryScopedProperties.SetScopeProperties(scopeProperties);

            var saveScoped = Logging?.ScopedLogger?.LogMetric == true;
            var saveScopedStream = Logging?.ScopedStreamLoggers?.Where(l => l.LogMetric).Any() == true;

            if (saveScoped || saveScopedStream) 
            {
                var messageData = new MetricMessage();
                metric(messageData);

                var allProperties = ConcatOnceIfProperties(properties);
                if (saveScoped)
                {
                    telemetryLogger.Metric(messageData.Message, messageData.Value, allProperties);
                }

                if (logToScopeStream && Logging?.ScopedStreamLoggers?.Count > 0)
                {
                    foreach (var scopedStreamLogger in Logging.ScopedStreamLoggers.Where(l => l.LogMetric))
                    {
                        telemetryScopedStreamLogger.Metric(scopedStreamLogger, messageData.Message, messageData.Value, allProperties);
                    }
                }
            }
        }

        public void SetScopeProperty(string key, string value)
        {
            if (!value.IsNullOrWhiteSpace())
            {
                telemetryScopedProperties.SetScopeProperty(new KeyValuePair<string, string>(key, value));
            }
        }

        public void SetUserScopeProperty(IEnumerable<Claim> claims)
        {
            var userId = claims.FindFirstValue(c => c.Type == JwtClaimTypes.Subject);
            if (!userId.IsNullOrWhiteSpace())
            {
                SetScopeProperty(Constants.Logs.UserId, userId);
            }

            var email = claims.FindFirstValue(c => c.Type == JwtClaimTypes.Email);
            if (!email.IsNullOrWhiteSpace())
            {
                SetScopeProperty(Constants.Logs.Email, email);
            }
        }

        private IDictionary<string, string> ConcatOnceIfProperties(IDictionary<string, string> properties)
        {
            return properties == null ? telemetryScopedProperties.Properties : new Dictionary<string, string>(telemetryScopedProperties.Properties).ConcatOnce(properties);
        }

        bool isDisposed = false;
        public void Dispose()
        {
            if(!isDisposed)
            {
                isDisposed = true;
                if (Logging != null && traceMessages.Count > 0)
                {
                    if (Logging.ScopedLogger != null)
                    {
                        var scopedLogger = Logging.ScopedLogger;
                        var telemetryLoggertraceMessages = traceMessages.Where(m =>
                            (m.TraceType == TraceTypes.Info && scopedLogger.LogInfoTrace) ||
                            (m.TraceType == TraceTypes.Claim && scopedLogger.LogClaimTrace) ||
                            (m.TraceType == TraceTypes.Message && scopedLogger.LogMessageTrace));
                        if (telemetryLoggertraceMessages.Count() > 0)
                        {
                            telemetryLogger.Trace(telemetryLoggertraceMessages.ToJson(), ConcatOnceIfProperties(new Dictionary<string, string> { { Constants.Logs.Type, nameof(TelemetryScopedLogger.ScopeTrace) } }));
                        }
                    }

                    if (Logging.ScopedStreamLoggers?.Count() > 0)
                    {
                        foreach (var scopedStreamLogger in Logging.ScopedStreamLoggers)
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
