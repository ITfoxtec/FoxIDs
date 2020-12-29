using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.UnitTests.Helpers
{
    public static class TelemetryLoggerHelper
    {
        public static TenantTrackLogger TraceLoggerObject(IHttpContextAccessor httpContextAccessor)
        {
            var tenantTrackLogger = new TenantTrackLogger(httpContextAccessor);
            return tenantTrackLogger;
        }

        public static TelemetryScopedLogger ScopedLoggerObject(Settings settings, IHttpContextAccessor httpContextAccessor)
        {
            var telemetryClient = new TelemetryClient(new TelemetryConfiguration("xxx"));
            var telemetryLogger = new TelemetryLogger(settings, telemetryClient);
            var tenantTrackLogger = new TenantTrackLogger(httpContextAccessor);
            var telemetryScopedProperties = new TelemetryScopedProperties();
            var telemetryScopedLogger = new TelemetryScopedLogger(telemetryLogger, telemetryScopedProperties, tenantTrackLogger);
            return telemetryScopedLogger;
        }
    }
}
