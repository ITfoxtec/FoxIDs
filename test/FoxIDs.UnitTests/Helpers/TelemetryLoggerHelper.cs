﻿using FoxIDs.Infrastructure;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.UnitTests.Helpers
{
    public static class TelemetryLoggerHelper
    {
        public static TelemetryScopedStreamLogger ScopedStreamLoggerObject(IHttpContextAccessor httpContextAccessor)
        {
            var telemetryScopedStreamLogger = new TelemetryScopedStreamLogger(httpContextAccessor);
            return telemetryScopedStreamLogger;
        }

        public static TelemetryScopedLogger ScopedLoggerObject(IHttpContextAccessor httpContextAccessor)
        {
            var telemetryClient = new TelemetryClient(new TelemetryConfiguration("xxx"));
            var telemetryLogger = new TelemetryLogger(telemetryClient);
            var telemetryScopedStreamLogger = new TelemetryScopedStreamLogger(httpContextAccessor);
            var telemetryScopedProperties = new TelemetryScopedProperties();
            var telemetryScopedLogger = new TelemetryScopedLogger(telemetryLogger, telemetryScopedProperties, telemetryScopedStreamLogger, httpContextAccessor);
            return telemetryScopedLogger;
        }
    }
}
