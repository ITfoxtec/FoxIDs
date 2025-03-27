using Microsoft.AspNetCore.Http;
using System;

namespace FoxIDs
{
    public static class HttpContextExtensions
    {
        public static string GetHost(this HttpContext context, bool addTrailingSlash = true)
        {
            return AddSlash($"{context.Request.Scheme}://{context.Request.Host.ToUriComponent()}/", addTrailingSlash);
        }

        public static Uri GetHostUri(this HttpContext context)
        {
            return new Uri(context.GetHost());
        }

        private static string AddSlash(string url, bool addTrailingSlash = true)
        {
            if (url.EndsWith('/'))
            {
                return addTrailingSlash ? url : url.Substring(0, url.Length - 1);
            }
            else
            {
                return addTrailingSlash ? $"{url}/" : url;
            }
        }
    }
}
