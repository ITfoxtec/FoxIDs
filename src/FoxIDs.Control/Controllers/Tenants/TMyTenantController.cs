using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net;
using FoxIDs.Logic;
using System;
using ITfoxtec.Identity;

namespace FoxIDs.Controllers
{
    public class TMyTenantController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;
        private readonly PlanCacheLogic planCacheLogic;
        private readonly TenantCacheLogic tenantCacheLogic;
        private readonly TrackCacheLogic trackCacheLogic;
        private readonly ExternalKeyLogic externalKeyLogic;

        public TMyTenantController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, PlanCacheLogic planCacheLogic, TenantCacheLogic tenantCacheLogic, TrackCacheLogic trackCacheLogic, ExternalKeyLogic externalKeyLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
            this.planCacheLogic = planCacheLogic;
            this.tenantCacheLogic = tenantCacheLogic;
            this.trackCacheLogic = trackCacheLogic;
            this.externalKeyLogic = externalKeyLogic;
        }

        /// <summary>
        /// Get my tenant.
        /// </summary>
        /// <returns>Tenant.</returns>
        [ProducesResponseType(typeof(Api.Tenant), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.Tenant>> GetMyTenant()
        {
            try
            {
                var MTenant = await tenantRepository.GetTenantByNameAsync(RouteBinding.TenantName);
                return Ok(mapper.Map<Api.Tenant>(MTenant));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get my '{typeof(Api.Tenant).Name}'.");
                    return NotFound(typeof(Api.Tenant).Name, RouteBinding.TenantName);
                }
                throw;
            }
        }

        /// <summary>
        /// Update my tenant.
        /// </summary>
        /// <param name="tenant">Tenant.</param>
        /// <returns>Tenant.</returns>
        [ProducesResponseType(typeof(Api.Tenant), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.Tenant>> PutMyTenant([FromBody] Api.MyTenantRequest tenant)
        {
            try
            {
                var mTenant = await tenantRepository.GetTenantByNameAsync(RouteBinding.TenantName);

                var invalidateCustomDomainInCache = (!mTenant.CustomDomain.IsNullOrEmpty() && !mTenant.CustomDomain.Equals(tenant.CustomDomain, StringComparison.OrdinalIgnoreCase)) ? mTenant.CustomDomain : null;

                if (!RouteBinding.PlanName.IsNullOrEmpty() && !tenant.CustomDomain.IsNullOrEmpty())
                {
                    var plan = await planCacheLogic.GetPlanAsync(RouteBinding.PlanName);
                    if (!plan.EnableCustomDomain)
                    {
                        throw new Exception($"Custom domain is not supported in the '{plan.Name}' plan.");
                    }
                }

                mTenant.CustomDomain = tenant.CustomDomain;
                mTenant.CustomDomainVerified = false;
                await tenantRepository.UpdateAsync(mTenant);

                await tenantCacheLogic.InvalidateTenantCacheAsync(RouteBinding.TenantName);
                if (!invalidateCustomDomainInCache.IsNullOrEmpty())
                {
                    await tenantCacheLogic.InvalidateCustomDomainCacheAsync(invalidateCustomDomainInCache);
                }

                return Ok(mapper.Map<Api.Tenant>(mTenant));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update my '{typeof(Api.Tenant).Name}'.");
                    return NotFound(typeof(Api.Tenant).Name, RouteBinding.TenantName);
                }
                throw;
            }
        }

        /// <summary>
        /// Delete my tenant.
        /// </summary>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteMyTenant()
        {
            try
            {
                if (RouteBinding.TenantName.Equals(Constants.Routes.MasterTenantName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("The master tenant can not be deleted.");
                }
     
                (var mTracks, _) = await tenantRepository.GetListAsync<Track>(new Track.IdKey { TenantName = RouteBinding.TenantName }, whereQuery: p => p.DataType.Equals("track"));
                foreach(var mTrack in mTracks)
                {
                    var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = mTrack.Name };
                    await tenantRepository.DeleteListAsync<DefaultElement>(trackIdKey);
                    await tenantRepository.DeleteAsync<Track>(mTrack.Id);

                    if (!mTrack.Key.ExternalName.IsNullOrWhiteSpace())
                    {
                        await externalKeyLogic.DeleteExternalKeyAsync(mTrack.Key.ExternalName);
                    }
                    await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);
                }
                var mTenant = await tenantRepository.DeleteAsync<Tenant>(await Tenant.IdFormatAsync(RouteBinding.TenantName));

                await tenantCacheLogic.InvalidateTenantCacheAsync(RouteBinding.TenantName);
                if (!string.IsNullOrEmpty(mTenant?.CustomDomain))
                {
                    await tenantCacheLogic.InvalidateCustomDomainCacheAsync(mTenant.CustomDomain);
                }

                return NoContent();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete my '{typeof(Api.Tenant).Name}'.");
                    return NotFound(typeof(Api.Tenant).Name, RouteBinding.TenantName);
                }
                throw;
            }
        }
    }
}
