using FoxIDs.Models;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Infrastructure
{
    public class TelemetryScopedProcessor : ITelemetryProcessor
    {
        private readonly Settings settings;
        private readonly ITelemetryProcessor next;
        private readonly IHttpContextAccessor httpContextAccessor;

        public TelemetryScopedProcessor(Settings settings, ITelemetryProcessor next, IHttpContextAccessor httpContextAccessor)
        {
            this.settings = settings;
            this.next = next;
            this.httpContextAccessor = httpContextAccessor;
        }

        public void Process(ITelemetry item)
        {
            if (item is ISupportProperties && !(item is ExceptionTelemetry) && !(item is MetricTelemetry))
            {
                if (httpContextAccessor.HttpContext != null && httpContextAccessor.HttpContext.RequestServices != null)
                {
                    var supportProperties = item as ISupportProperties;
                    if (supportProperties.Properties.Any(i => i.Key == Constants.Logs.Type && i.Value == nameof(TelemetryScopedLogger.ScopeTrace)))
                    {
                        supportProperties.Properties.Remove(Constants.Logs.Type);
                    }
                    else
                    {
                        var telemetryScopedProperties = httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedProperties>();
                        if (telemetryScopedProperties.Properties.Count > 0)
                        {
                            foreach (var prop in telemetryScopedProperties.Properties)
                            {
                                supportProperties.Properties[prop.Key] = prop.Value;
                            }
                        }
                    }
                }
            }

            if (settings.Options.Log == LogOptions.Stdout)
            {
                if (item is EventTelemetry eventTelemetry)
                {
                    GetLogger().LogInformation(GetEventTelemetryLogString(eventTelemetry));
                }
                else if (item is TraceTelemetry traceTelemetry)
                {
                    GetLogger().LogTrace(GetTraceTelemetryLogString(traceTelemetry));
                }
                else if (item is ExceptionTelemetry exceptionTelemetry)
                {
                    switch (exceptionTelemetry.SeverityLevel)
                    {
                        case SeverityLevel.Warning:
                            GetLogger().LogWarning(GetExceptionTelemetryLogString(exceptionTelemetry));
                            break;
                        case SeverityLevel.Critical:
                            GetLogger().LogCritical(GetExceptionTelemetryLogString(exceptionTelemetry));
                            break;
                        case SeverityLevel.Error:
                        default:
                            GetLogger().LogError(GetExceptionTelemetryLogString(exceptionTelemetry));
                            break;
                    }
                }
            }
            else if (settings.Options.Log == LogOptions.ApplicationInsights)
            {
                if (item is ExceptionTelemetry exceptionTelemetry)
                {
                    if (!exceptionTelemetry.Properties.ContainsKey(Constants.Logs.LoggingHandledKey))
                    {
                        if (httpContextAccessor.HttpContext != null && httpContextAccessor.HttpContext.RequestServices != null)
                        {
                            var routeBinding = httpContextAccessor.HttpContext.GetRouteBinding();
                            if (routeBinding != null)
                            {
                                var telemetryScopedLogger = httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedLogger>();
                                var telemetryScopedProperties = httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedProperties>();

                                ProcessScopeStreamLogs(exceptionTelemetry, routeBinding, telemetryScopedLogger, telemetryScopedProperties);

                                if (exceptionTelemetry.SeverityLevel == SeverityLevel.Warning)
                                {
                                    telemetryScopedLogger.Warning(exceptionTelemetry.Exception, telemetryScopedProperties.Properties, logToScopeStream: false);
                                    return;
                                }
                                else if (exceptionTelemetry.SeverityLevel == SeverityLevel.Error)
                                {
                                    telemetryScopedLogger.Error(exceptionTelemetry.Exception, telemetryScopedProperties.Properties, logToScopeStream: false);
                                    return;
                                }
                                else if (exceptionTelemetry.SeverityLevel == SeverityLevel.Critical)
                                {
                                    telemetryScopedLogger.CriticalError(exceptionTelemetry.Exception, telemetryScopedProperties.Properties, logToScopeStream: false);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                throw new NotSupportedException();
            }
            
            next.Process(item);
        }

        private ILogger<TelemetryScopedProcessor> GetLogger()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                .AddFilter((f) => true)
                .AddConsole();
            });
            return loggerFactory.CreateLogger<TelemetryScopedProcessor>();
        }

        private List<string> AddTelemetry(List<string> log, ITelemetry eventTelemetry)
        {
            log.Add($"Timestamps: {eventTelemetry.Timestamp}");
            log.Add($"RoleInstance: {eventTelemetry.Context?.Cloud?.RoleInstance}");
            log.Add($"OperationName: {eventTelemetry.Context?.Operation?.Name}");
            log.Add($"ClientIP: {eventTelemetry.Context?.Location?.Ip}");

            if(eventTelemetry is ISupportProperties supportProperties)
            {
                log.Add($"Properties: {supportProperties.Properties?.ToJson()}");
            }
            return log;
        }

        private string GetEventTelemetryLogString(EventTelemetry eventTelemetry)
        {
            var log = new List<string>
            {
                eventTelemetry.Name
            };
            log = AddTelemetry(log, eventTelemetry);
            return string.Join(Environment.NewLine, log);
        }

        private string GetTraceTelemetryLogString(TraceTelemetry traceTelemetry)
        {
            var log = new List<string>
            {
                traceTelemetry.Message
            };
            log = AddTelemetry(log, traceTelemetry);
            return string.Join(Environment.NewLine, log);
        }
        private string GetExceptionTelemetryLogString(ExceptionTelemetry exceptionTelemetry)
        {
            var log = new List<string>();
            if (exceptionTelemetry.Exception == null)
            {
                log.Add(exceptionTelemetry.Message);
            }
            else
            {
                log.Add(exceptionTelemetry.Exception.ToString());
            }
            log = AddTelemetry(log, exceptionTelemetry);
            return string.Join(Environment.NewLine, log);
        }
        private void ProcessScopeStreamLogs(ExceptionTelemetry exceptionTelemetry, RouteBinding routeBinding, TelemetryScopedLogger telemetryScopedLogger, TelemetryScopedProperties telemetryScopedProperties)
        {
            if (routeBinding.Logging?.ScopedStreamLoggers?.Count() > 0)
            {
                var telemetryScopedStreamLogger = httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedStreamLogger>();

                if (exceptionTelemetry.SeverityLevel == SeverityLevel.Warning)
                {
                    foreach (var scopedStreamLogger in routeBinding.Logging.ScopedStreamLoggers.Where(l => l.LogWarning))
                    {
                        telemetryScopedStreamLogger.Warning(telemetryScopedLogger, scopedStreamLogger, exceptionTelemetry.Exception, telemetryScopedProperties.Properties);
                    }
                }
                if (exceptionTelemetry.SeverityLevel == SeverityLevel.Error)
                {
                    foreach (var scopedStreamLogger in routeBinding.Logging.ScopedStreamLoggers.Where(l => l.LogWarning))
                    {
                        telemetryScopedStreamLogger.Error(telemetryScopedLogger, scopedStreamLogger, exceptionTelemetry.Exception, telemetryScopedProperties.Properties);
                    }
                }
                if (exceptionTelemetry.SeverityLevel == SeverityLevel.Critical)
                {
                    foreach (var scopedStreamLogger in routeBinding.Logging.ScopedStreamLoggers.Where(l => l.LogWarning))
                    {
                        telemetryScopedStreamLogger.CriticalError(telemetryScopedLogger, scopedStreamLogger, exceptionTelemetry.Exception, telemetryScopedProperties.Properties);
                    }
                }
            }
        }
    }
}
