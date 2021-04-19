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

        public void Warning(TrackLogger trackLogger, Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            Warning(trackLogger, exception, null, properties, metrics);
        }
        public void Warning(TrackLogger trackLogger, Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            //TODO implement tenant track logging
        }

        public void Error(TrackLogger trackLogger, Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            Error(trackLogger, exception, null, properties, metrics);
        }
        public void Error(TrackLogger trackLogger, Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            //TODO implement tenant track logging
        }

        public void CriticalError(TrackLogger trackLogger, Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            CriticalError(trackLogger, exception, null, properties, metrics);
        }
        public void CriticalError(TrackLogger trackLogger, Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            //TODO implement tenant track logging
        }

        public void Event(TrackLogger trackLogger, string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            //TODO implement tenant track logging
        }
        public void Trace(TrackLogger trackLogger, string message, IDictionary<string, string> properties = null)
        {
            //TODO implement tenant track logging
        }

        public void Metric(TrackLogger trackLogger, string message, double value, IDictionary<string, string> properties = null)
        {
            //TODO implement tenant metric logging
        }
    }
}
