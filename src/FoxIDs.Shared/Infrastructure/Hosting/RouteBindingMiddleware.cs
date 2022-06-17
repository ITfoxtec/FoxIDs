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
using FoxIDs.Logic;

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
                    var customDomain = httpContext.Items[Constants.Routes.RouteBindingCustomDomainHeader] as string;
                    var hasCustomDomain = !customDomain.IsNullOrEmpty();
                    CheckCustomDomainSupport(hasCustomDomain);

                    var trackIdKey = GetTrackIdKey(route, hasCustomDomain);
                    if (trackIdKey != null)
                    {
                        var routeBinding = await GetRouteDataAsync(scopedLogger, httpContext.RequestServices, trackIdKey, hasCustomDomain, customDomain, GetPartyNameAndbinding(route, hasCustomDomain), AcceptUnknownParty(httpContext.Request.Path.Value, route));
                        httpContext.Items[Constants.Routes.RouteBindingKey] = routeBinding;
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

        protected virtual void CheckCustomDomainSupport(bool hasCustomDomain) { }

        protected virtual ValueTask SeedAsync(IServiceProvider requestServices) => default;

        protected virtual ValueTask<bool> PreAsync(HttpContext httpContext, string[] route) => new ValueTask<bool>(true);

        protected abstract Track.IdKey GetTrackIdKey(string[] route, bool hasCustomDomain);

        protected virtual bool AcceptUnknownParty(string path, string[] route) => false;

        protected virtual string GetPartyNameAndbinding(string[] route, bool hasCustomDomain) => null;

        protected virtual ValueTask<RouteBinding> PostRouteDataAsync(TelemetryScopedLogger scopedLogger, IServiceProvider requestServices, Track.IdKey trackIdKey, Track track, RouteBinding routeBinding, string partyNameAndBinding, bool acceptUnknownParty) => new ValueTask<RouteBinding>(routeBinding);

        private async Task<RouteBinding> GetRouteDataAsync(TelemetryScopedLogger scopedLogger, IServiceProvider requestServices, Track.IdKey trackIdKey, bool hasCustomDomain, string customDomain, string partyNameAndBinding, bool acceptUnknownParty)
        {
            if (hasCustomDomain)
            {
                var tenantCacheLogic = requestServices.GetService<TenantCacheLogic>();               
                trackIdKey.TenantName = await tenantCacheLogic.GetTenantNameByCustomDomain(customDomain);
            }

            var track = await GetTrackAsync(trackIdKey, hasCustomDomain);
            scopedLogger.SetScopeProperty(Constants.Logs.TenantName, trackIdKey.TenantName);
            scopedLogger.SetScopeProperty(Constants.Logs.TrackName, trackIdKey.TrackName);
            var routeBinding = new RouteBinding
            {
                HasCustomDomain = hasCustomDomain,
                RouteUrl = $"{(!hasCustomDomain ? $"{trackIdKey.TenantName}/" : string.Empty)}{trackIdKey.TrackName}{(!partyNameAndBinding.IsNullOrWhiteSpace() ? $"/{partyNameAndBinding}" : string.Empty)}",
                TenantName = trackIdKey.TenantName,
                TrackName = trackIdKey.TrackName,
                Resources = track.Resources,
            };

            return await PostRouteDataAsync(scopedLogger, requestServices, trackIdKey, track, routeBinding, partyNameAndBinding, acceptUnknownParty);
        }

        private async Task<Track> GetTrackAsync(Track.IdKey idKey, bool hasCustomDomain)
        {
            try
            {
                return await tenantRepository.GetTrackByNameAsync(idKey);
            }
            catch (Exception ex)
            {
                if (hasCustomDomain && idKey.TenantName.Equals(idKey.TrackName, StringComparison.OrdinalIgnoreCase)) 
                {
                    throw new RouteCreationException($"Invalid tenant and track name '{idKey.TenantName}'. The URL for a custom domain has to be without the tenant element.", ex);
                }
                throw new RouteCreationException($"Invalid tenant name '{idKey.TenantName}' and track name '{idKey.TrackName}'.", ex);
            }
        }
    }
}
