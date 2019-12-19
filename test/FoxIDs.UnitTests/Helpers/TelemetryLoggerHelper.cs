using FoxIDs.Infrastructure;
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

        public static TelemetryScopedLogger ScopedLoggerObject(IHttpContextAccessor httpContextAccessor)
        {
            var telemetryClient = new TelemetryClient(new TelemetryConfiguration("xxx"));
            var telemetryLogger = new TelemetryLogger(telemetryClient);
            var tenantTrackLogger = new TenantTrackLogger(httpContextAccessor);
            var telemetryScopedProperties = new TelemetryScopedProperties();
            var telemetryScopedLogger = new TelemetryScopedLogger(telemetryLogger, telemetryScopedProperties, tenantTrackLogger);
            return telemetryScopedLogger;
        }
    }
}
