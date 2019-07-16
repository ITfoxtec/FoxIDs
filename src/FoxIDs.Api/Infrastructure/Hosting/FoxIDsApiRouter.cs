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

            if (pathSplit.Length >= 3 && pathSplit.Length <= 4)
            {
                await HandleTenantAndMasterRouteAsync(context, context.HttpContext.Request.Method, pathSplit);
            }
            else
            {
                throw new Exception($"Invalid route path.");
            }
        }

        private async Task HandleTenantAndMasterRouteAsync(RouteContext context, string method, string[] path)
        {
            var trackIdKey = new Track.IdKey();
            var controllerPreName = string.Empty;
            if (Constants.Routes.MasterApiName.Equals(path[1], StringComparison.InvariantCultureIgnoreCase))
            {
                controllerPreName = Constants.Routes.ApiControllerPreMasterName;
                trackIdKey.TenantName = Constants.Routes.MasterTenantName;
                trackIdKey.TrackName = Constants.Routes.MasterTrackName;
            }
            else
            {
                controllerPreName = Constants.Routes.ApiControllerPreTenantName;
                trackIdKey.TenantName = path[1].ToLower();
                trackIdKey.TrackName = path[2].ToLower();
            }

            var scopedLogger = context.HttpContext.RequestServices.GetService<TelemetryScopedLogger>();
            try
            {
                var routeBinding = await GetRouteDataAsync(scopedLogger, context.HttpContext, trackIdKey);
                context.RouteData.DataTokens[Constants.Routes.RouteBindingKey] = routeBinding;

                scopedLogger.SetScopeProperty(Constants.Routes.RouteBindingKey, new { routeBinding.TenantName, routeBinding.TrackName }.ToJson());

                var routeController = path[path.Length - 1];
                context.RouteData.Values[Constants.Routes.RouteControllerKey] = $"{controllerPreName}{routeController}";
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
