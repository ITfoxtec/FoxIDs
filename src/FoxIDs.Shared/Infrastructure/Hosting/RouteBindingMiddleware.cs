using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using FoxIDs.Models;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using FoxIDs.Logic;
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
            try
            {
                await SeedAsync(httpContext.RequestServices);

                var route = httpContext.Request.Path.Value.Split('/').Where(r => !r.IsNullOrWhiteSpace()).ToArray();

                if (await PreAsync(httpContext, route))
                {
                    var customDomain = httpContext.Items[Constants.Routes.RouteBindingCustomDomainHeader] as string;
                    var useCustomDomain = GetUseCustomDomain(route, customDomain);

                    var trackIdKey = GetTrackIdKey(route, useCustomDomain);
                    if (trackIdKey != null)
                    {
                        var scopedLogger = httpContext.RequestServices.GetService<TelemetryScopedLogger>();
                        var routeBinding = await GetRouteDataAsync(scopedLogger, httpContext.RequestServices, trackIdKey, useCustomDomain, customDomain, GetPartyNameAndbinding(route, useCustomDomain), AcceptUnknownParty(httpContext.Request.Path.Value, route));
                        httpContext.Items[Constants.Routes.RouteBindingKey] = routeBinding;
                    }

                    await next(httpContext);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failing route request path '{httpContext.Request.Path.Value}'.", ex);
            }
        }

        private bool GetUseCustomDomain(string[] route, string customDomain)
        {
            var hasCustomDomain = !customDomain.IsNullOrEmpty();
            if (hasCustomDomain && !CheckCustomDomainSupport(route))
            {
                return false;
            }

            return hasCustomDomain;
        }

        protected abstract bool CheckCustomDomainSupport(string[] route);

        protected virtual ValueTask SeedAsync(IServiceProvider requestServices) => default;

        protected virtual ValueTask<bool> PreAsync(HttpContext httpContext, string[] route) => new ValueTask<bool>(true);

        protected abstract Track.IdKey GetTrackIdKey(string[] route, bool useCustomDomain);

        protected virtual bool AcceptUnknownParty(string path, string[] route) => false;

        protected virtual string GetPartyNameAndbinding(string[] route, bool useCustomDomain) => null;

        protected virtual ValueTask<RouteBinding> PostRouteDataAsync(TelemetryScopedLogger scopedLogger, IServiceProvider requestServices, Track.IdKey trackIdKey, Track track, RouteBinding routeBinding, string partyNameAndBinding, bool acceptUnknownParty) => new ValueTask<RouteBinding>(routeBinding);

        private async Task<RouteBinding> GetRouteDataAsync(TelemetryScopedLogger scopedLogger, IServiceProvider requestServices, Track.IdKey trackIdKey, bool useCustomDomain, string customDomain, string partyNameAndBinding, bool acceptUnknownParty)
        {
            var tenant = await GetTenantAsync(requestServices, useCustomDomain, customDomain, trackIdKey.TenantName);
            if (useCustomDomain)
            {
                trackIdKey.TenantName = tenant.Name;
            }

            var plan = await GetPlanAsync(requestServices, tenant.PlanName);
            if (plan != null)
            {
                if (useCustomDomain && !plan.EnableCustomDomain)
                {
                    throw new Exception($"Custom domain is not supported in the '{plan.Name}' plan.");
                }
            }

            var track = await GetTrackAsync(scopedLogger, requestServices, trackIdKey, useCustomDomain);
            scopedLogger.SetScopeProperty(Constants.Logs.TenantName, trackIdKey.TenantName);
            scopedLogger.SetScopeProperty(Constants.Logs.TrackName, trackIdKey.TrackName);
            var hasVerifiedCustomDomain = !tenant.CustomDomain.IsNullOrEmpty() && tenant.CustomDomainVerified;
            var routeBinding = new RouteBinding
            {
                HasVerifiedCustomDomain = hasVerifiedCustomDomain,
                UseCustomDomain = useCustomDomain && hasVerifiedCustomDomain,
                CustomDomain = tenant.CustomDomain,
                RouteUrl = $"{(!useCustomDomain ? $"{trackIdKey.TenantName}/" : string.Empty)}{trackIdKey.TrackName}{(!partyNameAndBinding.IsNullOrWhiteSpace() ? $"/{partyNameAndBinding}" : string.Empty)}",
                PlanName = plan?.Name,
                TenantName = trackIdKey.TenantName,
                TrackName = trackIdKey.TrackName,
                Resources = track.Resources,
                ShowResourceId = track.ShowResourceId,
                PlanLogLifetime = plan?.LogLifetime,
            };

            return await PostRouteDataAsync(scopedLogger, requestServices, trackIdKey, track, routeBinding, partyNameAndBinding, acceptUnknownParty);
        }

        private static async Task<Tenant> GetTenantAsync(IServiceProvider serviceProvider, bool useCustomDomain, string customDomain, string tenantName)
        {
            var tenantCacheLogic = serviceProvider.GetService<TenantCacheLogic>();
            if (useCustomDomain)
            {
                return await tenantCacheLogic.GetTenantByCustomDomainAsync(customDomain);
            }
            else
            {
                return await tenantCacheLogic.GetTenantAsync(tenantName);
            }
        }

        private async Task<Plan> GetPlanAsync(IServiceProvider serviceProvider, string planName)
        {
            if (planName.IsNullOrEmpty())
            {
                return null;
            }
            var planCacheLogic = serviceProvider.GetService<PlanCacheLogic>();
            return await planCacheLogic.GetPlanAsync(planName, required: false);
        }

        private async Task<Track> GetTrackAsync(TelemetryScopedLogger scopedLogger, IServiceProvider serviceProvider, Track.IdKey idKey, bool useCustomDomain)
        {
            try
            {
                var track = await trackCacheLogic.GetTrackAsync(idKey);
                if(track.Key.Type == TrackKeyTypes.ContainedRenewSelfSigned)
                {
                    var containedKeyLogic = serviceProvider.GetService<ContainedKeyLogic>();
                    track = await containedKeyLogic.RenewCertificateAsync(idKey, track);

                }
                else if (track.Key.Type == TrackKeyTypes.KeyVaultRenewSelfSigned)
                {
                    var externalKeyLogic = serviceProvider.GetService<ExternalKeyLogic>();
                    track = await externalKeyLogic.PhasedOutExternalKeyAsync(scopedLogger, idKey, track);
                }
                return track;
            }
            catch (Exception ex)
            {
                if (ex is FoxIDsDataException cex)
                {
                    if (cex.InnerException is CosmosException)
                    {
                        if (useCustomDomain && idKey.TenantName.Equals(idKey.TrackName, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new RouteCreationException($"Invalid tenant and environment '{idKey.TenantName}'. The URL for a custom domain has to be without the tenant element.", ex);
                        }
                        throw new RouteCreationException($"Invalid tenant '{idKey.TenantName}' and environment '{idKey.TrackName}'.", ex);
                    }
                }

                if (useCustomDomain && idKey.TenantName.Equals(idKey.TrackName, StringComparison.OrdinalIgnoreCase)) 
                {
                    throw new RouteCreationException($"Error loading tenant and environment '{idKey.TenantName}'.", ex);
                }
                throw new RouteCreationException($"Error loading tenant '{idKey.TenantName}' and environment '{idKey.TrackName}'.", ex);
            }
        }
    }
}
