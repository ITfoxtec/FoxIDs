﻿using FoxIDs.Models.Config;
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
            if (!(IsHealthCheck(context) || IsLoopback(context)))
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

        protected override bool IsHealthCheck(HttpContext context)
        {
            if (context.Request?.Path == "/" || $"/{Constants.Routes.HealthController}".Equals(context.Request?.Path, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
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
    }
}
