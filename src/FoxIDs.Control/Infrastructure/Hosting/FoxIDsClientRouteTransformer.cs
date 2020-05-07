using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsClientRouteTransformer : SiteRouteTransformer
    {
        protected override string MapPath(string path)
        {
            return path;
        }

        protected override async Task<RouteValueDictionary> HandleRouteAsync(HttpContext httpContext, RouteValueDictionary values, string[] route)
        {
            values[Constants.Routes.RouteControllerKey] = Constants.Routes.DefaultClientController;
            values[Constants.Routes.RouteActionKey] = Constants.Routes.DefaultSiteAction;
            return await Task.FromResult(values);
        }
    }
}
