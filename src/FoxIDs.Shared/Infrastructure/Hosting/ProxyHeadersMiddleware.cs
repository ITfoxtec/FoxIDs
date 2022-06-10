using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Hosting
{
    public class ProxyHeadersMiddleware
    {
        private readonly RequestDelegate next;

        public ProxyHeadersMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            ClientIp(context);
            Host(context);

            await next.Invoke(context);
        }

        private static void ClientIp(HttpContext context)
        {
            string ipHeader = context.Request.Headers["CF-Connecting-IP"];
            if (ipHeader.IsNullOrWhiteSpace())
            {
                ipHeader = context.Request.Headers["X-Forwarded-For"];
            }
            if (!ipHeader.IsNullOrWhiteSpace())
            {
                IPAddress ipAddress;
                if (IPAddress.TryParse(ipHeader, out ipAddress))
                {
                    context.Connection.RemoteIpAddress = ipAddress;
                }
            }
        }

        private static void Host(HttpContext context)
        {
            string hostHeader = context.Request.Headers["X-ORIGINAL-HOST"];
            if (hostHeader.IsNullOrWhiteSpace())
            {
                hostHeader = context.Request.Headers["X-Forwarded-Host"];
            }
            if (!hostHeader.IsNullOrWhiteSpace())
            {
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
