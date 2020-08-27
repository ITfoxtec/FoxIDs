using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsApiRouteTransformer : SiteRouteTransformer
    {
        protected override string MapPath(string path)
        {
            return path;
        }

        protected override async Task<RouteValueDictionary> HandleRouteAsync(HttpContext httpContext, RouteValueDictionary values, string[] route)
        {
            if (route.Length >= 2 && route.Length <= 4)
            {
                await HandleMasterAndTenantTrackRouteAsync(httpContext, httpContext.Request.Method, values, route);
            }
            else
            {
                throw new NotSupportedException($"Api route '{string.Join('/', route)}' not supported.");
            }

            return values;
        }

        private async Task HandleMasterAndTenantTrackRouteAsync(HttpContext httpContext, string method, RouteValueDictionary values, string[] route)
        {
            var routeController = GetRouteController(route);
            values[Constants.Routes.RouteControllerKey] = routeController;
            values[Constants.Routes.RouteActionKey] = $"{method.ToLower()}{routeController.Substring(1)}";
            await Task.CompletedTask;
        }

        private string GetRouteController(string[] route)
        {
            if (route.Length >= 2 && route[0].Equals(Constants.Routes.MasterApiName, StringComparison.InvariantCultureIgnoreCase) && route[1].StartsWith(Constants.Routes.PreApikey))
            {
                return route[1].Replace(Constants.Routes.PreApikey, Constants.Routes.ApiControllerPreMasterKey);
            }
            else if (route.Length >= 2 && route[1].StartsWith(Constants.Routes.PreApikey))
            {
                return route[1].Replace(Constants.Routes.PreApikey, Constants.Routes.ApiControllerPreTenantTrackKey);
            }
            else if (route.Length >= 3 && route[2].StartsWith(Constants.Routes.PreApikey))
            {
                return route[2].Replace(Constants.Routes.PreApikey, Constants.Routes.ApiControllerPreTenantTrackKey);
            }
            else
            {
                throw new NotSupportedException($"Api route '{string.Join('/', route)}' not supported.");
            }
        }
    }
}
