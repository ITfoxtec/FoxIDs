using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using FoxIDs.Models;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using FoxIDs.Logic;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using FoxIDs.Repository;
using Microsoft.Azure.Cosmos;

namespace FoxIDs.Infrastructure.Hosting
{
    public abstract class RouteBindingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly TrackCacheLogic trackCacheLogic;

        public RouteBindingMiddleware(RequestDelegate next, TrackCacheLogic trackCacheLogic)
        {
            this.next = next;
            this.trackCacheLogic = trackCacheLogic;
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
                    if (hasCustomDomain && !CheckCustomDomainSupport(route))
                    {
                        hasCustomDomain = false;
                    }

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

        protected abstract bool CheckCustomDomainSupport(string[] route);

        protected virtual ValueTask SeedAsync(IServiceProvider requestServices) => default;

        protected virtual ValueTask<bool> PreAsync(HttpContext httpContext, string[] route) => new ValueTask<bool>(true);

        protected abstract Track.IdKey GetTrackIdKey(string[] route, bool hasCustomDomain);

        protected virtual bool AcceptUnknownParty(string path, string[] route) => false;

        protected virtual string GetPartyNameAndbinding(string[] route, bool hasCustomDomain) => null;

        protected virtual ValueTask<RouteBinding> PostRouteDataAsync(TelemetryScopedLogger scopedLogger, IServiceProvider requestServices, Track.IdKey trackIdKey, Track track, RouteBinding routeBinding, string partyNameAndBinding, bool acceptUnknownParty) => new ValueTask<RouteBinding>(routeBinding);

        private async Task<RouteBinding> GetRouteDataAsync(TelemetryScopedLogger scopedLogger, IServiceProvider requestServices, Track.IdKey trackIdKey, bool hasCustomDomain, string customDomain, string partyNameAndBinding, bool acceptUnknownParty)
        {
            var tenant = await GetTenantAsync(requestServices, hasCustomDomain, customDomain, trackIdKey.TenantName);
            if (hasCustomDomain)
            {
                trackIdKey.TenantName = tenant.Name;
            }

            var plan = await GetPlanAsync(requestServices, tenant.PlanName);
            if (plan != null)
            {
                if (hasCustomDomain && !plan.EnableCustomDomain)
                {
                    throw new Exception($"Custom domain not enabled by plan '{plan.Name}'.");
                }
            }

            var track = await GetTrackAsync(trackIdKey, hasCustomDomain);
            scopedLogger.SetScopeProperty(Constants.Logs.TenantName, trackIdKey.TenantName);
            scopedLogger.SetScopeProperty(Constants.Logs.TrackName, trackIdKey.TrackName);
            var routeBinding = new RouteBinding
            {
                HasCustomDomain = hasCustomDomain,
                RouteUrl = $"{(!hasCustomDomain ? $"{trackIdKey.TenantName}/" : string.Empty)}{trackIdKey.TrackName}{(!partyNameAndBinding.IsNullOrWhiteSpace() ? $"/{partyNameAndBinding}" : string.Empty)}",
                PlanName = plan?.Name,
                TenantName = trackIdKey.TenantName,
                TrackName = trackIdKey.TrackName,
                Resources = track.Resources,
                TelemetryClient = GetTelmetryClient(plan?.ApplicationInsightsConnectionString),
                LogAnalyticsWorkspaceId = plan?.LogAnalyticsWorkspaceId
            };

            return await PostRouteDataAsync(scopedLogger, requestServices, trackIdKey, track, routeBinding, partyNameAndBinding, acceptUnknownParty);
        }

        private TelemetryClient GetTelmetryClient(string applicationInsightsConnectionString)
        {
            if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
            {
                return new TelemetryClient(new TelemetryConfiguration { ConnectionString = applicationInsightsConnectionString });
            }
            else
            {
                return null;
            }
        }

        private static async Task<Tenant> GetTenantAsync(IServiceProvider requestServices, bool hasCustomDomain, string customDomain, string tenantName)
        {
            var tenantCacheLogic = requestServices.GetService<TenantCacheLogic>();
            if (hasCustomDomain)
            {
                return await tenantCacheLogic.GetTenantByCustomDomainAsync(customDomain);
            }
            else
            {
                return await tenantCacheLogic.GetTenantAsync(tenantName);
            }
        }

        private async Task<Plan> GetPlanAsync(IServiceProvider requestServices, string planName)
        {
            if (planName.IsNullOrEmpty())
            {
                return null;
            }
            var planCacheLogic = requestServices.GetService<PlanCacheLogic>();
            return await planCacheLogic.GetPlanAsync(planName, required: false);
        }

        private async Task<Track> GetTrackAsync(Track.IdKey idKey, bool hasCustomDomain)
        {
            try
            {
                return await trackCacheLogic.GetTrackAsync(idKey);
            }
            catch (Exception ex)
            {
                if (ex is CosmosDataException cex)
                {
                    if (cex.InnerException is CosmosException)
                    {
                        if (hasCustomDomain && idKey.TenantName.Equals(idKey.TrackName, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new RouteCreationException($"Invalid tenant and track '{idKey.TenantName}'. The URL for a custom domain has to be without the tenant element.", ex);
                        }
                        throw new RouteCreationException($"Invalid tenant '{idKey.TenantName}' and track '{idKey.TrackName}'.", ex);
                    }
                }

                if (hasCustomDomain && idKey.TenantName.Equals(idKey.TrackName, StringComparison.OrdinalIgnoreCase)) 
                {
                    throw new RouteCreationException($"Error loading tenant and track '{idKey.TenantName}'.", ex);
                }
                throw new RouteCreationException($"Error loading tenant '{idKey.TenantName}' and track '{idKey.TrackName}'.", ex);
            }
        }
    }
}
