using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsRouteBindingMiddleware : RouteBindingMiddleware
    {
        public FoxIDsRouteBindingMiddleware(RequestDelegate next, ITenantRepository tenantRepository) : base(next, tenantRepository)
        { }

        protected override ValueTask SeedAsync(IServiceProvider requestServices) => default;        

        protected override Track.IdKey GetTrackIdKey(string[] route)
        {
            if (route.Length >= 1 && route[0].Equals(Constants.Routes.DefaultSiteController, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            else if (route.Length >= 2)
            {
                return new Track.IdKey
                {
                    TenantName = route[0].ToLower(),
                    TrackName = route[1].ToLower()
                };
            }
            else
            {
                throw new NotSupportedException($"FoxIDs route '{string.Join('/', route)}' not supported.");
            }
        }

        protected override string GetPartyNameAndbinding(string[] route)
        {
            if (route.Length >= 3)
            {
                return route[2].ToLower();
            }
            else
            {
                return null;
            }
        }
    }
}
