using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Hosting
{
    public class ErrorLoggingMiddleware
    {
        protected readonly RequestDelegate next;

        public ErrorLoggingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var scopedLogger = httpContext.RequestServices.GetService<TelemetryScopedLogger>();
            try
            {
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
        }
    }
}
