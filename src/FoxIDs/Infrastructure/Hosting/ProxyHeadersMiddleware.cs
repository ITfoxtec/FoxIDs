using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Hosting
{
    public class ProxyHeadersMiddleware : ProxyClientIpHeadersMiddleware
    {
        public ProxyHeadersMiddleware(RequestDelegate next) : base(next) { }    

        public override async Task Invoke(HttpContext context)
        {
            var clientIpIsLoopback = ClientIp(context);
            if (!clientIpIsLoopback)
            {
                var host = ReadHostFromHeader(context);
                if (!host.IsNullOrWhiteSpace())
                {
                    context.Items[Constants.Routes.RouteBindingCustomDomainHeader] = host;
                }
                else
                {
                    var settings = context.RequestServices.GetService<FoxIDsSettings>();
                    if (settings.RequestDomainAsCustomDomain)
                    {
                        context.Items[Constants.Routes.RouteBindingCustomDomainHeader] = context.Request.Host.Host;
                    }
                }
            }

            await next.Invoke(context);
        }

        private bool Secret(HttpContext context)
        {
            var settings = context.RequestServices.GetService<FoxIDsSettings>();
            if (!settings.ProxySecret.IsNullOrEmpty())
            {
                string secretHeader = context.Request.Headers["X-FoxIDs-Secret"];
                if (!settings.ProxySecret.Equals(secretHeader, StringComparison.Ordinal))
                {
                    throw new Exception("Proxy secret in 'X-FoxIDs-Secret' header not accepted.");
                }
                return true;
            }
            return settings.TrustProxyHeaders;
        }

        private string ReadHostFromHeader(HttpContext context)
        {
            var trustProxyHeaders = Secret(context);
            if (trustProxyHeaders)
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

            return string.Empty;
        }
    }
}
