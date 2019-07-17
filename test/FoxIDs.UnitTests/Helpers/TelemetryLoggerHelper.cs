using FoxIDs.Infrastructure;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.UnitTests.Helpers
{
    public static class TelemetryLoggerHelper
    {
        public static TenantTrackLogger TraceLoggerObject(IHttpContextAccessor httpContextAccessor)
        {
            var telemetryClient = new TelemetryClient();
            var telemetryLogger = new TelemetryLogger(telemetryClient);
            var tenantTrackLogger = new TenantTrackLogger(httpContextAccessor);
            return tenantTrackLogger;
        }

        public static TelemetryScopedLogger ScopedLoggerObject(IHttpContextAccessor httpContextAccessor)
        {
            var telemetryClient = new TelemetryClient();
            var telemetryLogger = new TelemetryLogger(telemetryClient);
            var tenantTrackLogger = new TenantTrackLogger(httpContextAccessor);
            var telemetryScopedLogger = new TelemetryScopedLogger(telemetryLogger, tenantTrackLogger);
            return telemetryScopedLogger;
        }
    }
}
