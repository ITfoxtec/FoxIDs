using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsClientRouteBindingMiddleware : RouteBindingMiddleware
    {
        public FoxIDsClientRouteBindingMiddleware(RequestDelegate next, ITenantRepository tenantRepository) : base(next, tenantRepository)
        { }

        protected override Track.IdKey GetTrackIdKey(string[] route)
        {
            var trackIdKey = new Track.IdKey();
            trackIdKey.TrackName = Constants.Routes.MasterTrackName;

            if (route.Length >= 1)
            {
                trackIdKey.TenantName = route[0].ToLower();
            }
            else
            {
                throw new NotSupportedException($"Client route '{string.Join('/', route)}' not supported.");
            }

            return trackIdKey;
        }

        protected override string GetPartyNameAndbinding(string[] route)
        {
            return null;
        }
    }
}
