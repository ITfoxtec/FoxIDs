using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
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
            var hasSecret = Secret(context);

            ClientIp(context);
            var host = Host(context);

            if (hasSecret && !host.IsNullOrWhiteSpace())
            {
                context.Items[Constants.Routes.RouteBindingCustomDomainHeader] = host;
            }

            await next.Invoke(context);
        }

        private bool Secret(HttpContext context)
        {
            var settings = context.RequestServices.GetService<Settings>();
            if (!settings.ProxySecret.IsNullOrEmpty())
            {
                string secretHeader = context.Request.Headers["X-FoxIDs-Secret"];
                if (!settings.ProxySecret.Equals(secretHeader, StringComparison.Ordinal))
                {
                    throw new Exception("Proxy secret in 'X-FoxIDs-Secret' header not accepted.");
                }
                return true;
            }
            return false;
        }

        private static void ClientIp(HttpContext context)
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
        }

        private static string Host(HttpContext context)
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
            return hostHeader;
        }
    }
}
