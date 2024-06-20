using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsClientRouteTransformer : SiteRouteTransformer
    {
        protected override bool CheckCustomDomainSupport(string[] route)
        {
            throw new NotSupportedException("Host in header not supported in Control Client.");
        }

        protected override string MapPath(string path)
        {
            return path;
        }

        protected override Task<RouteValueDictionary> HandleRouteAsync(HttpContext httpContext, bool useCustomDomain, RouteValueDictionary values, string[] route)
        {
            values[Constants.Routes.RouteControllerKey] = Constants.Routes.DefaultSiteController;

            if (route.Length == 2 &&
                route[route.Length - 2].Equals(Constants.Routes.DefaultSiteController, StringComparison.InvariantCultureIgnoreCase) &&
                route[route.Length - 1].Equals(Constants.Routes.ErrorAction, StringComparison.InvariantCultureIgnoreCase))
            {
                values[Constants.Routes.RouteActionKey] = Constants.Routes.ErrorAction;
            }
            else
            {
                values[Constants.Routes.RouteActionKey] = Constants.Routes.DefaultAction;
            }

            return Task.FromResult(values);
        }
    }
}
