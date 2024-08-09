//using FoxIDs.Infrastructure.Logging;
//using FoxIDs.Models;
//using FoxIDs.Models.Config;
//using Microsoft.ApplicationInsights.Channel;
//using Microsoft.ApplicationInsights.DataContracts;
//using Microsoft.ApplicationInsights.Extensibility;
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using System;
//using System.Linq;

//namespace FoxIDs.Infrastructure
//{
//    public class TelemetryScopedProcessor : ITelemetryProcessor
//    {
//        private readonly Settings settings;
//        private readonly IWebHostEnvironment environment;
//        private readonly StdoutTelemetryLogger stdoutTelemetryLogger;
//        private readonly ITelemetryProcessor next;
//        private readonly IHttpContextAccessor httpContextAccessor;

//        public TelemetryScopedProcessor(Settings settings, IWebHostEnvironment environment, StdoutTelemetryLogger stdoutTelemetryLogger, ITelemetryProcessor next, IHttpContextAccessor httpContextAccessor)
//        {
//            this.settings = settings;
//            this.environment = environment;
//            this.stdoutTelemetryLogger = stdoutTelemetryLogger;
//            this.next = next;
//            this.httpContextAccessor = httpContextAccessor;
//        }

//        public void Process(ITelemetry item)
//        {
//            if (item is ISupportProperties && !(item is ExceptionTelemetry) && !(item is MetricTelemetry))
//            {
//                if (httpContextAccessor.HttpContext != null && httpContextAccessor.HttpContext.RequestServices != null)
//                {
//                    var supportProperties = item as ISupportProperties;
//                    if (supportProperties.Properties.Any(i => i.Key == Constants.Logs.Type && i.Value == nameof(TelemetryScopedLogger.ScopeTrace)))
//                    {
//                        supportProperties.Properties.Remove(Constants.Logs.Type);
//                    }
//                    else
//                    {
//                        var telemetryScopedProperties = httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedProperties>();
//                        if (telemetryScopedProperties.Properties.Count > 0)
//                        {
//                            foreach (var prop in telemetryScopedProperties.Properties)
//                            {
//                                supportProperties.Properties[prop.Key] = prop.Value;
//                            }
//                        }
//                    }
//                }
//            }

//            if (environment.IsDevelopment() || settings.Options.Log == LogOptions.Stdout || settings.Options.Log == LogOptions.OpenSearchAndStdoutErrors)
//            {
//                //var telemetryScopedProperties = httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedProperties>();
//                //if(settings.Options.Log == LogOptions.Stdout || settings.Options.Log == LogOptions.OpenSearchAndStdoutErrors)
//                //{
//                //    ProcessScopeStreamLogs(exceptionTelemetry, routeBinding, telemetryScopedLogger, telemetryScopedProperties);
//                //}

//                if (item is EventTelemetry eventTelemetry)
//                {
//                    stdoutTelemetryLogger.LogInformation(eventTelemetry);
//                }
//                else if (item is TraceTelemetry traceTelemetry)
//                {
//                    if (!traceTelemetry.Message.StartsWith("AI:", StringComparison.Ordinal))
//                    {
//                        stdoutTelemetryLogger.LogTrace(traceTelemetry);
//                    }
//                }
//                else if (item is ExceptionTelemetry exceptionTelemetry)
//                {
//                    switch (exceptionTelemetry.SeverityLevel)
//                    {
//                        case SeverityLevel.Warning:
//                            stdoutTelemetryLogger.LogWarning(exceptionTelemetry);
//                            break;
//                        case SeverityLevel.Critical:
//                            stdoutTelemetryLogger.LogCritical(exceptionTelemetry);
//                            break;
//                        case SeverityLevel.Error:
//                        default:
//                            stdoutTelemetryLogger.LogError(exceptionTelemetry);
//                            break;
//                    }
//                }
//            }
            
//            if (settings.Options.Log == LogOptions.ApplicationInsights)
//            {
//                if (item is ExceptionTelemetry exceptionTelemetry)
//                {
//                    if (!exceptionTelemetry.Properties.ContainsKey(Constants.Logs.LoggingHandledKey))
//                    {
//                        if (httpContextAccessor.HttpContext != null && httpContextAccessor.HttpContext.RequestServices != null)
//                        {
//                            var routeBinding = httpContextAccessor.HttpContext.GetRouteBinding();
//                            if (routeBinding != null)
//                            {
//                                var telemetryScopedLogger = httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedLogger>();
//                                var telemetryScopedProperties = httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedProperties>();

//                                ProcessScopeStreamLogs(exceptionTelemetry, routeBinding, telemetryScopedLogger, telemetryScopedProperties);

//                                if (exceptionTelemetry.SeverityLevel == SeverityLevel.Warning)
//                                {
//                                    telemetryScopedLogger.Warning(exceptionTelemetry.Exception, telemetryScopedProperties.Properties, logToScopeStream: false);
//                                    return;
//                                }
//                                else if (exceptionTelemetry.SeverityLevel == SeverityLevel.Error)
//                                {
//                                    telemetryScopedLogger.Error(exceptionTelemetry.Exception, telemetryScopedProperties.Properties, logToScopeStream: false);
//                                    return;
//                                }
//                                else if (exceptionTelemetry.SeverityLevel == SeverityLevel.Critical)
//                                {
//                                    telemetryScopedLogger.CriticalError(exceptionTelemetry.Exception, telemetryScopedProperties.Properties, logToScopeStream: false);
//                                    return;
//                                }
//                            }
//                        }
//                    }
//                }
//            }
            
//            next.Process(item);
//        }

//        private void ProcessScopeStreamLogs(ExceptionTelemetry exceptionTelemetry, RouteBinding routeBinding, TelemetryScopedLogger telemetryScopedLogger, TelemetryScopedProperties telemetryScopedProperties)
//        {
//            if (routeBinding.Logging?.ScopedStreamLoggers?.Count() > 0)
//            {
//                var telemetryScopedStreamLogger = httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedStreamLogger>();

//                if (exceptionTelemetry.SeverityLevel == SeverityLevel.Warning)
//                {
//                    foreach (var scopedStreamLogger in routeBinding.Logging.ScopedStreamLoggers.Where(l => l.LogWarning))
//                    {
//                        telemetryScopedStreamLogger.Warning(telemetryScopedLogger, scopedStreamLogger, exceptionTelemetry.Exception, telemetryScopedProperties.Properties);
//                    }
//                }
//                if (exceptionTelemetry.SeverityLevel == SeverityLevel.Error)
//                {
//                    foreach (var scopedStreamLogger in routeBinding.Logging.ScopedStreamLoggers.Where(l => l.LogWarning))
//                    {
//                        telemetryScopedStreamLogger.Error(telemetryScopedLogger, scopedStreamLogger, exceptionTelemetry.Exception, telemetryScopedProperties.Properties);
//                    }
//                }
//                if (exceptionTelemetry.SeverityLevel == SeverityLevel.Critical)
//                {
//                    foreach (var scopedStreamLogger in routeBinding.Logging.ScopedStreamLoggers.Where(l => l.LogWarning))
//                    {
//                        telemetryScopedStreamLogger.CriticalError(telemetryScopedLogger, scopedStreamLogger, exceptionTelemetry.Exception, telemetryScopedProperties.Properties);
//                    }
//                }
//            }
//        }
//    }
//}
