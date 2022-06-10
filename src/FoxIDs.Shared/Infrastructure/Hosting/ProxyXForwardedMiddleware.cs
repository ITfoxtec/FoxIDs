using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace FoxIDs.Infrastructure.Hosting
{
    public class ProxyXForwardedMiddleware
    {
        private readonly RequestDelegate next;

        public ProxyXForwardedMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            XForwardedFor(context);
            XForwardedHost(context);

            await next.Invoke(context);
        }

        private static void XForwardedFor(HttpContext context)
        {
            string ipHeader = context.Request.Headers["X-Forwarded-For"];
            if (!ipHeader.IsNullOrWhiteSpace())
            {
                IPAddress ipAddress;
                if (IPAddress.TryParse(ipHeader, out ipAddress))
                {
                    context.Connection.RemoteIpAddress = ipAddress;
                }
            }
        }

        private static void XForwardedHost(HttpContext context)
        {
            var logger = context.RequestServices.GetService<TelemetryLogger>();

            try
            {
                throw new System.Exception("All headers " + string.Join(", ", context.Request.Headers.Select(h => h)));
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
            }

            string hostHeader = context.Request.Headers["X-Forwarded-Host"];
            if (!hostHeader.IsNullOrWhiteSpace())
            {
                try
                {
                    throw new System.Exception("X-Forwarded-Host: " + hostHeader);
                }
                catch (System.Exception ex)
                {
                    logger.Error(ex);
                }

                if (context.Request.Host.Port.HasValue)
                {
                    context.Request.Host = new HostString(hostHeader, context.Request.Host.Port.Value);
                }
                else
                {
                    context.Request.Host = new HostString(hostHeader);
                }
            }
        }
    }
}
