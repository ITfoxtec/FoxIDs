using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System;
using UrlCombineLib;

namespace FoxIDs
{
    public static class HttpContextExtensions
    {
        public static string GetHost(this HttpContext context)
        {
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
    }
}
