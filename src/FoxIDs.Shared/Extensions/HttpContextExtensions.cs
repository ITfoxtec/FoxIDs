using Microsoft.AspNetCore.Http;

namespace FoxIDs
{
    public static class HttpContextExtensions
    {
        public static string GetHost(this HttpContext context)
        {
            return $"{context.Request.Scheme}://{context.Request.Host.ToUriComponent()}/";
        }
    }
}
