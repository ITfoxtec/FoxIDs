using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using UrlCombineLib;

namespace FoxIDs
{
    public static class HttpContextExtensions
    {
        public static string GetHost(this HttpContext context)
        {
            var settings = context.RequestServices.GetService<Settings>();
            if (settings != null)
            {
                if (!settings.FoxIDsControlEndpoint.IsNullOrEmpty())
                {
                    return AddSlash(settings.FoxIDsControlEndpoint);
                }
                if (!settings.FoxIDsEndpoint.IsNullOrEmpty())
                {
                    return AddSlash(settings.FoxIDsEndpoint);
                } 
            }

            return $"{context.Request.Scheme}://{context.Request.Host.ToUriComponent()}/";
        }

        public static string GetHostWithTenantAndTrack(this HttpContext context)
        {
            var routeBinding = context.GetRouteBinding();
            if (!routeBinding.HasCustomDomain)
            {
                return UrlCombine.Combine(context.GetHost(), routeBinding.TenantName, routeBinding.TrackName);
            }
            else
            {
                return UrlCombine.Combine(context.GetHost(), routeBinding.TrackName);
            }
        }

        public static Uri GetHostUri(this HttpContext context)
        {
            return new Uri(context.GetHost());
        }

        private static string AddSlash(string url)
        {
            if (url.EndsWith('/'))
            {
                return url;
            }
            else
            {
                return $"{url}/";
            }
        }
    }
}
