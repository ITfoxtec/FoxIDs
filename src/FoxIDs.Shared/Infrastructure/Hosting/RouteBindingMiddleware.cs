using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using FoxIDs.Models;
using System.Linq;
using FoxIDs.Repository;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Infrastructure.Hosting
{
    public abstract class RouteBindingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ITenantRepository tenantRepository;

        public RouteBindingMiddleware(RequestDelegate next, ITenantRepository tenantRepository)
        {
            this.next = next;
            this.tenantRepository = tenantRepository;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var scopedLogger = httpContext.RequestServices.GetService<TelemetryScopedLogger>();
            try
            {
                await SeedAsync(httpContext.RequestServices);

                var route = httpContext.Request.Path.Value.Split('/').Where(r => !r.IsNullOrWhiteSpace()).ToArray();

                if (await PreAsync(httpContext, route))
                {
                    var trackIdKey = GetTrackIdKey(route);
                    if (trackIdKey != null)
                    {
                        var routeBinding = await GetRouteDataAsync(scopedLogger, httpContext.RequestServices, trackIdKey, GetPartyNameAndbinding(route));
                        httpContext.Items[Constants.Routes.RouteBindingKey] = routeBinding;

                        scopedLogger.SetScopeProperty(Constants.Routes.RouteBindingKey, new { routeBinding.TenantName, routeBinding.TrackName }.ToJson());
                    }

                    await next(httpContext);
                }
            }
            catch (ValidationException vex)
            {
                scopedLogger.Error(vex, $"Failing route request path '{httpContext.Request.Path.Value}'.");
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failing route request path '{httpContext.Request.Path.Value}'.", ex);
            }
        }

        protected abstract ValueTask SeedAsync(IServiceProvider requestServices);

        protected abstract ValueTask<bool> PreAsync(HttpContext httpContext, string[] route);

        protected abstract Track.IdKey GetTrackIdKey(string[] route);

        protected abstract string GetPartyNameAndbinding(string[] route);

        protected abstract ValueTask<RouteBinding> PostRouteDataAsync(TelemetryScopedLogger scopedLogger, IServiceProvider requestServices, Track.IdKey trackIdKey, Track track, RouteBinding routeBinding, string partyNameAndBinding = null);

        private async Task<RouteBinding> GetRouteDataAsync(TelemetryScopedLogger scopedLogger, IServiceProvider requestServices, Track.IdKey trackIdKey, string partyNameAndBinding = null)
        {
            var track = await GetTrackAsync(tenantRepository, trackIdKey);
            var routeBinding = new RouteBinding
            {
                RouteUrl = $"{trackIdKey.TenantName}/{trackIdKey.TrackName}{(!partyNameAndBinding.IsNullOrWhiteSpace() ? $"/{partyNameAndBinding}" : string.Empty)}",
                TenantName = trackIdKey.TenantName,
                TrackName = trackIdKey.TrackName,
                Resources = track.Resources,
            };

            return await PostRouteDataAsync(scopedLogger, requestServices, trackIdKey, track, routeBinding, partyNameAndBinding);
        }

        private async Task<Track> GetTrackAsync(ITenantRepository tenantRepository, Track.IdKey idKey)
        {
            try
            {
                return await tenantRepository.GetTrackByNameAsync(idKey);
            }
            catch (Exception ex)
            {
                throw new RouteCreationException($"Invalid tenantName '{idKey.TenantName}' and trackName '{idKey.TrackName}'.", ex);
            }
        }
    }
}
