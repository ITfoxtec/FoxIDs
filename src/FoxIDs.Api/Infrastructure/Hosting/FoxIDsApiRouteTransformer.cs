using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using FoxIDs.Models;
using System.ComponentModel.DataAnnotations;
using ITfoxtec.Identity;
using FoxIDs.Repository;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsApiRouteTransformer : SiteRouteTransformer
    {
        private readonly IServiceProvider serviceProvider;

        public FoxIDsApiRouteTransformer(IServiceProvider serviceProvider, ITenantRepository tenantRepository) : base(tenantRepository)
        {
            this.serviceProvider = serviceProvider;
        }

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
            var trackIdKey = new Track.IdKey();
            trackIdKey.TrackName = Constants.Routes.DefaultMasterTrackName;
            string routeController;

            if (route.Length >= 2 && route[0].Equals(Constants.Routes.MasterApiName, StringComparison.InvariantCultureIgnoreCase) && route[1].StartsWith(Constants.Routes.PreApikey))
            {
                routeController = route[1].Replace(Constants.Routes.PreApikey, Constants.Routes.ApiControllerPreMasterKey);
                trackIdKey.TenantName = Constants.Routes.MasterTenantName;
            }
            else if (route.Length >= 2 && route[1].StartsWith(Constants.Routes.PreApikey))
            {
                routeController = route[1].Replace(Constants.Routes.PreApikey, Constants.Routes.ApiControllerPreTenantTrackKey);
                trackIdKey.TenantName = route[0].ToLower();
            }
            else if (route.Length >= 3 && route[2].StartsWith(Constants.Routes.PreApikey))
            {
                routeController = route[2].Replace(Constants.Routes.PreApikey, Constants.Routes.ApiControllerPreTenantTrackKey);
                trackIdKey.TenantName = route[0].ToLower();
                trackIdKey.TrackName = route[1].ToLower();
            }
            else
            {
                throw new NotSupportedException($"Api route '{string.Join('/', route)}' not supported.");
            }

            var scopedLogger = serviceProvider.GetService<TelemetryScopedLogger>();
            try
            {
                var routeBinding = await GetRouteDataAsync(scopedLogger, trackIdKey);
                httpContext.Items[Constants.Routes.RouteBindingKey] = routeBinding;

                scopedLogger.SetScopeProperty(Constants.Routes.RouteBindingKey, new { routeBinding.TenantName, routeBinding.TrackName }.ToJson());

                values[Constants.Routes.RouteControllerKey] = routeController;
                values[Constants.Routes.RouteActionKey] = $"{method.ToLower()}{routeController.Substring(1)}";
            }
            catch (ValidationException vex)
            {
                scopedLogger.Error(vex);
                throw;
            }
        }
    }
}
