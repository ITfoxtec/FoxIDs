using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace FoxIDs.Infrastructure
{
    public class TelemetryScopedProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor next;
        private readonly IHttpContextAccessor httpContextAccessor;

        public TelemetryScopedProcessor(ITelemetryProcessor next, IHttpContextAccessor httpContextAccessor)
        {
            this.next = next;
            this.httpContextAccessor = httpContextAccessor;
        }

        public void Process(ITelemetry item)
        {
            if (item is ISupportProperties && !(item is MetricTelemetry))
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

            if (item is ExceptionTelemetry exceptionTelemetry)
            {
                if (httpContextAccessor.HttpContext != null && httpContextAccessor.HttpContext.RequestServices != null)
                {
                    var routeBinding = httpContextAccessor.HttpContext.GetRouteBinding();
                    if(routeBinding?.Logging?.ScopedStreamLoggers?.Count() > 0)
                    {
                        var telemetryScopedStreamLogger = httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedStreamLogger>();
                        var telemetryScopedProperties = httpContextAccessor.HttpContext.RequestServices.GetService<TelemetryScopedProperties>();

                        if (exceptionTelemetry.SeverityLevel == SeverityLevel.Warning)
                        {
                            foreach (var scopedStreamLogger in routeBinding.Logging.ScopedStreamLoggers.Where(l => l.LogWarning))
                            {
                                telemetryScopedStreamLogger.Warning(scopedStreamLogger, exceptionTelemetry.Exception, telemetryScopedProperties.Properties);
                            }
                        }
                        if (exceptionTelemetry.SeverityLevel == SeverityLevel.Error)
                        {
                            foreach (var scopedStreamLogger in routeBinding.Logging.ScopedStreamLoggers.Where(l => l.LogWarning))
                            {
                                telemetryScopedStreamLogger.Error(scopedStreamLogger, exceptionTelemetry.Exception, telemetryScopedProperties.Properties);
                            }
                        }
                        if (exceptionTelemetry.SeverityLevel == SeverityLevel.Critical)
                        {
                            foreach (var scopedStreamLogger in routeBinding.Logging.ScopedStreamLoggers.Where(l => l.LogWarning))
                            {
                                telemetryScopedStreamLogger.CriticalError(scopedStreamLogger, exceptionTelemetry.Exception, telemetryScopedProperties.Properties);
                            }
                        }
                    }
                }
            }

            next.Process(item);
        }
    }
}
