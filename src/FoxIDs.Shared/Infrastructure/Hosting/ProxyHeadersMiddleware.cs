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
        protected readonly RequestDelegate next;

        public ProxyHeadersMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public virtual async Task Invoke(HttpContext context)
        {
            if (!IsHealthCheckOrLoopback(context))
            {
                ReadClientIp(context);
                _ = ValidateProxySecret(context);
            }

            await next.Invoke(context);
        }

        protected bool IsHealthCheckOrLoopback(HttpContext context)
        {
            if (context.Request?.Path == "/" || $"/{Constants.Routes.HealthPageName}".Equals(context.Request?.Path, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (context.Connection?.RemoteIpAddress != null)
            {
                return IPAddress.IsLoopback(context.Connection.RemoteIpAddress);
            }
            return false;
        }

        protected bool ValidateProxySecret(HttpContext context, Settings settings = null)
        {
            settings = settings ?? context.RequestServices.GetService<Settings>();
            if (!settings.ProxySecret.IsNullOrEmpty())
            {
                string secretHeader = context.Request.Headers["X-FoxIDs-Secret"];
                if (secretHeader.IsNullOrEmpty())
                {
                    secretHeader = context.Request.Query["X-FoxIDs-Secret"];
                }
                if (!settings.ProxySecret.Equals(secretHeader, StringComparison.Ordinal))
                {
                    throw new Exception("Proxy secret in 'X-FoxIDs-Secret' header or query not accepted.");
                }
                return true;
            }
            return false;
        }

        protected void ReadClientIp(HttpContext context)
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
    }
}
