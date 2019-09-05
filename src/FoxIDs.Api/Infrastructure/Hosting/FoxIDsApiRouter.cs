using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using FoxIDs.Models;
using System.ComponentModel.DataAnnotations;
using ITfoxtec.Identity;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsApiRouter : SiteRouter
    {
        public FoxIDsApiRouter(IRouter defaultRouter) : base(defaultRouter)
        { }

        protected override async Task HandleRouteAsync(RouteContext context)
        {
            var pathSplit = context.HttpContext.Request.Path.Value.Split('/');

            if (pathSplit.Length >= 3 && pathSplit.Length <= 5)
            {
                await HandleMasterAndTenantTrackRouteAsync(context, context.HttpContext.Request.Method, pathSplit);
            }
            else
            {
               // throw new NotSupportedException($"Invalid route path. Api url '{context.HttpContext.Request.Path.Value}' not supported.");
            }
        }

        private async Task HandleMasterAndTenantTrackRouteAsync(RouteContext context, string method, string[] path)
        {
            var trackIdKey = new Track.IdKey();
            trackIdKey.TrackName = Constants.Routes.DefaultMasterTrackName;
            string routeController;

            if (path.Length >= 3 && path[1].Equals(Constants.Routes.MasterApiName, StringComparison.InvariantCultureIgnoreCase) && path[2].StartsWith(Constants.Routes.PreApikey))
            {
                routeController = path[2].Replace(Constants.Routes.PreApikey, Constants.Routes.ApiControllerPreMasterKey);
                trackIdKey.TenantName = Constants.Routes.MasterTenantName;
            }
            else if (path.Length >= 3 && path[2].StartsWith(Constants.Routes.PreApikey))
            {
                routeController = path[2].Replace(Constants.Routes.PreApikey, Constants.Routes.ApiControllerPreTenantTrackKey);
                trackIdKey.TenantName = path[1].ToLower();
            }
            else if (path.Length >= 4 && path[3].StartsWith(Constants.Routes.PreApikey))
            {
                routeController = path[3].Replace(Constants.Routes.PreApikey, Constants.Routes.ApiControllerPreTenantTrackKey);
                trackIdKey.TenantName = path[1].ToLower();
                trackIdKey.TrackName = path[2].ToLower();
            }
            else
            {
                throw new NotSupportedException($"Api url '{context.HttpContext.Request.Path.Value}' not supported.");
            }

            var scopedLogger = context.HttpContext.RequestServices.GetService<TelemetryScopedLogger>();
            try
            {
                var routeBinding = await GetRouteDataAsync(scopedLogger, context.HttpContext, trackIdKey);
                context.RouteData.DataTokens[Constants.Routes.RouteBindingKey] = routeBinding;

                scopedLogger.SetScopeProperty(Constants.Routes.RouteBindingKey, new { routeBinding.TenantName, routeBinding.TrackName }.ToJson());

                context.RouteData.Values[Constants.Routes.RouteControllerKey] = routeController;
                context.RouteData.Values[Constants.Routes.RouteActionKey] = method.ToLower();
            }
            catch (ValidationException vex)
            {
                scopedLogger.Error(vex);
                throw;
            }
        }
    }
}
