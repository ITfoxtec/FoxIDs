using FoxIDs.Logic.Seed;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsClientRouteBindingMiddleware : RouteBindingMiddleware
    {
        public FoxIDsClientRouteBindingMiddleware(RequestDelegate next, ITenantRepository tenantRepository) : base(next, tenantRepository)
        { }

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

        protected override Track.IdKey GetTrackIdKey(string[] route)
        {
            if (route.Length >= 1)
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
