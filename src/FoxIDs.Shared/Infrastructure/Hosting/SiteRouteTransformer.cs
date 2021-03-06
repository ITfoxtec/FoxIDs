using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using System.Linq;

namespace FoxIDs.Infrastructure.Hosting
{
    public abstract class SiteRouteTransformer : DynamicRouteValueTransformer
    {        
        public override async ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
        {
            try
            {
                var path = MapPath(values[Constants.Routes.RouteTransformerPathKey] is string ? values[Constants.Routes.RouteTransformerPathKey] as string : string.Empty);
                var route = path.Split('/').Where(r => !r.IsNullOrWhiteSpace()).ToArray();
                return await HandleRouteAsync(httpContext, values, route);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failing URL '{httpContext.Request.Scheme}://{httpContext.Request.Host.ToUriComponent()}{httpContext.Request.Path.Value}'", ex);
            }
        }

        protected abstract string MapPath(string path);

        protected abstract Task<RouteValueDictionary> HandleRouteAsync(HttpContext httpContext, RouteValueDictionary values, string[] route);
    }
}
