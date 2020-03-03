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
                    if (supportProperties.Properties.Any(i => i.Key == "type" && i.Value == nameof(TelemetryScopedLogger.ScopeTrace)))
                    {
                        supportProperties.Properties.Remove("type");
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

            next.Process(item);
        }
    }
}
