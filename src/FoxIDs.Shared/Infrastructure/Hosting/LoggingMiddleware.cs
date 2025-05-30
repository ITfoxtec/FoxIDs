﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Hosting
{
    public class LoggingMiddleware
    {
        protected readonly RequestDelegate next;

        public LoggingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var activity = new Activity("HttpRequest");
            activity.Start();

            var scopedLogger = httpContext.RequestServices.GetService<TelemetryScopedLogger>();
            try
            {
                if (scopedLogger != null)
                {
                    scopedLogger.SetScopeProperty(Constants.Logs.MachineName, Environment.MachineName);
                    scopedLogger.SetScopeProperty(Constants.Logs.OperationId, Activity.Current.TraceId.ToString());
                    scopedLogger.SetScopeProperty(Constants.Logs.RequestId, httpContext.TraceIdentifier);
                    scopedLogger.SetScopeProperty(Constants.Logs.RequestPath, httpContext.Request.Path);
                    scopedLogger.SetScopeProperty(Constants.Logs.RequestMethod, httpContext.Request.Method);
                    scopedLogger.SetScopeProperty(Constants.Logs.UserAgent, httpContext.Request.Headers["User-Agent"].ToString());
                }

                await next(httpContext);
            }
            catch (Exception ex)
            {
                if (scopedLogger != null)
                {
                    scopedLogger.Error(ex);
                }
                else
                {
                    var logger = httpContext.RequestServices.GetService<TelemetryLogger>();
                    logger.Error(ex);
                }
            }
            finally
            {
                activity.Stop();
            }
        }
    }
}
