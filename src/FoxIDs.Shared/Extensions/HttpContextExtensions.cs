using Microsoft.AspNetCore.Http;
using System;

namespace FoxIDs
{
    public static class HttpContextExtensions
    {
        public static string GetHost(this HttpContext context)
        {
            return $"{context.Request.Scheme}://{context.Request.Host.ToUriComponent()}/";
        }
        public static Uri GetHostUri(this HttpContext context)
        {
            return new Uri(context.GetHost());
        }

    }
}
