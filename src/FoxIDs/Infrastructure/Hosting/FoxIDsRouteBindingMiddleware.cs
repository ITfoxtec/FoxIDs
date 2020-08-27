﻿using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsRouteBindingMiddleware : RouteBindingMiddleware
    {
        public FoxIDsRouteBindingMiddleware(RequestDelegate next, ITenantRepository tenantRepository) : base(next, tenantRepository)
        { }

        protected override Track.IdKey GetTrackIdKey(string[] route)
        {
            return new Track.IdKey
            {
                TenantName = route[0].ToLower(),
                TrackName = route[1].ToLower()
            };
        }

        protected override string GetPartyNameAndbinding(string[] route)
        {
            return route[2].ToLower();
        }
    }
}
