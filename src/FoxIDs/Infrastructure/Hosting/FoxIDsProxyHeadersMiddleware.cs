using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsProxyHeadersMiddleware : ProxyHeadersMiddleware
    {
        public FoxIDsProxyHeadersMiddleware(RequestDelegate next) : base(next) { }    

        public override async Task Invoke(HttpContext context)
        {
            if (!IsHealthCheckOrLoopback(context))
            {
                ReadClientIp(context);
                ReadSchemeFromHeader(context);
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

        private bool TrustProxyHeaders(HttpContext context)
        {
            var settings = context.RequestServices.GetService<FoxIDsSettings>();
            if (settings.TrustProxyHeaders)
            {
                return true;
            }

            return ValidateProxySecret(context, settings);
        }

        private string ReadHostFromHeader(HttpContext context)
        {
            var trustProxyHeaders = TrustProxyHeaders(context);
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

        private void ReadSchemeFromHeader(HttpContext context)
        {
            var settings = context.RequestServices.GetService<FoxIDsSettings>();
            if (settings.TrustProxySchemeHeader)
            {
                string schemeHeader = context.Request.Headers["X-Forwarded-Scheme"];
                if (schemeHeader.IsNullOrWhiteSpace())
                {
                    schemeHeader = context.Request.Headers["X-Forwarded-Proto"];
                }
                if (!schemeHeader.IsNullOrWhiteSpace())
                {
                    if (schemeHeader.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Request.Scheme = Uri.UriSchemeHttp;
                    }
                    else if(schemeHeader.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Request.Scheme = Uri.UriSchemeHttps;
                    }
                }
            }
        }
    }
}
