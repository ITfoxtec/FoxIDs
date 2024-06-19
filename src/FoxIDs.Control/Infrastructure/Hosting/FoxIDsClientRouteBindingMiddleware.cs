using FoxIDs.Logic;
using FoxIDs.Logic.Seed;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsClientRouteBindingMiddleware : RouteBindingMiddleware
    {
        public FoxIDsClientRouteBindingMiddleware(RequestDelegate next, TrackCacheLogic trackCacheLogic) : base(next, trackCacheLogic)
        { }

        protected override bool CheckCustomDomainSupport(string[] route)
        {
            throw new NotSupportedException("Host in header not supported in Control Client.");
        }

        protected override async ValueTask SeedAsync(IServiceProvider requestServices) => await requestServices.GetService<SeedLogic>().SeedAsync();

        protected override ValueTask<bool> PreAsync(HttpContext httpContext, string[] route)
        {
            if (route.Length == 0)
            {
                httpContext.Response.Redirect(Constants.Routes.MasterTenantName);
                return new ValueTask<bool>(false);
            }
            return new ValueTask<bool>(true);
        }

        protected override Track.IdKey GetTrackIdKey(string[] route, bool useCustomDomain)
        {
            if (route.Length == 2 &&
                route[route.Length - 2].Equals(Constants.Routes.DefaultSiteController, StringComparison.InvariantCultureIgnoreCase) && 
                route[route.Length - 1].Equals(Constants.Routes.ErrorAction, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            else if (route.Length >= 1)
            {
                return new Track.IdKey
                {
                    TrackName = Constants.Routes.MasterTrackName,
                    TenantName = route[0].ToLower()
                };
            }
            else
            {
                throw new NotSupportedException($"FoxIDs client route '{string.Join('/', route)}' not supported.");
            }
        }
    }
}
