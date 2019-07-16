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

        public void Warning(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            Warning(exception, null, properties, metrics);
        }
        public void Warning(Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            var routeBinding = httpContextAccessor.HttpContext.TryGetRouteBinding();
            if(routeBinding != null)
            {
                //TODO implement tenant track logging
            }
        }

        public void Error(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            Error(exception, null, properties, metrics);
        }
        public void Error(Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            var routeBinding = httpContextAccessor.HttpContext.TryGetRouteBinding();
            if (routeBinding != null)
            {
                //TODO implement tenant track logging
            }
        }

        public void CriticalError(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            CriticalError(exception, null, properties, metrics);
        }
        public void CriticalError(Exception exception, string message, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            var routeBinding = httpContextAccessor.HttpContext.TryGetRouteBinding();
            if (routeBinding != null)
            {
                //TODO implement tenant track logging
            }
        }

        public void Event(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            var routeBinding = httpContextAccessor.HttpContext.TryGetRouteBinding();
            if (routeBinding != null)
            {
                //TODO implement tenant track logging
            }
        }
        public void Trace(string message, IDictionary<string, string> properties = null)
        {
            var routeBinding = httpContextAccessor.HttpContext.TryGetRouteBinding();
            if (routeBinding != null)
            {
                //TODO implement tenant track logging
            }
        }
    }
}
