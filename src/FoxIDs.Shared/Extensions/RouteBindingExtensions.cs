using FoxIDs.Models;
using Microsoft.AspNetCore.Http;

namespace FoxIDs
{
    public static class RouteBindingExtensions
    {
        public static RouteBinding GetRouteBinding(this HttpContext httpContext)
        {
            return httpContext.Items[Constants.Routes.RouteBindingKey] as RouteBinding;
        }
        public static RouteBinding TryGetRouteBinding(this HttpContext httpContext)
        {
            try
            {
                return httpContext.GetRouteBinding();
            }
            catch { }

            return null;
        }

        public static string GetRouteSequenceString(this HttpContext httpContext)
        {
            return httpContext.Items[Constants.Routes.SequenceStringKey] as string;
        }
    }
}
