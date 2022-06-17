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
        private readonly MasterTenantLogic masterTenantLogic;
        private readonly TenantCacheLogic tenantCacheLogic;
        private readonly ExternalKeyLogic externalKeyLogic;

        public TMyTenantController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, MasterTenantLogic masterTenantLogic, TenantCacheLogic tenantCacheLogic, ExternalKeyLogic externalKeyLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
            this.masterTenantLogic = masterTenantLogic;
            this.tenantCacheLogic = tenantCacheLogic;
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

                mTenant.CustomDomain = tenant.CustomDomain;
                mTenant.CustomDomainVerified = false;
                await tenantRepository.UpdateAsync(mTenant);

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
     
                var mTracks = await tenantRepository.GetListAsync<Track>(new Track.IdKey { TenantName = RouteBinding.TenantName }, whereQuery: p => p.DataType.Equals("track"));
                foreach(var mTrack in mTracks)
                {
                    await tenantRepository.DeleteListAsync<DefaultElement>(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = mTrack.Name });
                    await tenantRepository.DeleteAsync<Track>(mTrack.Id);

                    if (mTrack.Key.Type == TrackKeyType.KeyVaultRenewSelfSigned)
                    {
                        await externalKeyLogic.DeleteExternalKeyAsync(mTrack.Key.ExternalName);
                    }
                }
                var mTenant = await tenantRepository.DeleteAsync<Tenant>(await Tenant.IdFormat(RouteBinding.TenantName));

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
