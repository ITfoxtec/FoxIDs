using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Hosting
{
    public class ProxyClientIpMiddleware
    {
        private readonly RequestDelegate next;

        public ProxyClientIpMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
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

            await next.Invoke(context);
        }
    }
}
