using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace FoxIDs.Infrastructure
{
    public class TenantTrackLogger
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public TenantTrackLogger(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public void Warning(ScopedStreamLogger trackLogger, Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            Warning(trackLogger, exception, null, properties, metrics);
        }
        public void Warning(ScopedStreamLogger trackLogger, Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            //TODO implement tenant track logging
        }

        public void Error(ScopedStreamLogger trackLogger, Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            Error(trackLogger, exception, null, properties, metrics);
        }
        public void Error(ScopedStreamLogger trackLogger, Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            //TODO implement tenant track logging
        }

        public void CriticalError(ScopedStreamLogger trackLogger, Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            CriticalError(trackLogger, exception, null, properties, metrics);
        }
        public void CriticalError(ScopedStreamLogger trackLogger, Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            //TODO implement tenant track logging
        }

        public void Event(ScopedStreamLogger trackLogger, string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            //TODO implement tenant track logging
        }
        public void Trace(ScopedStreamLogger trackLogger, string message, IDictionary<string, string> properties = null)
        {
            //TODO implement tenant track logging
        }

        public void Metric(ScopedStreamLogger trackLogger, string message, double value, IDictionary<string, string> properties = null)
        {
            //TODO implement tenant metric logging
        }
    }
}
