using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Hosting
{
    public class ProxyClientIpHeadersMiddleware
    {
        protected readonly RequestDelegate next;

        public ProxyClientIpHeadersMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public virtual async Task Invoke(HttpContext context)
        {
            ClientIp(context);

            await next.Invoke(context);
        }

        protected bool ClientIp(HttpContext context)
        {
            string ipHeader = context.Request.Headers["CF-Connecting-IP"];
            if (ipHeader.IsNullOrWhiteSpace())
            {
                ipHeader = context.Request.Headers["X-Azure-ClientIP"];
            }
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

            return IPAddress.IsLoopback(context.Connection.RemoteIpAddress);
        }
    }
}
