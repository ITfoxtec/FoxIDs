using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Logic;
using System;
using ITfoxtec.Identity;
using FoxIDs.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using FoxIDs.Models.Config;
using System.Linq;
using FoxIDs.Logic.Usage;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TMyTenantController :  ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IMapper mapper;
        private readonly IMasterDataRepository masterDataRepository;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly PlanCacheLogic planCacheLogic;
        private readonly TenantCacheLogic tenantCacheLogic;
        private readonly TrackCacheLogic trackCacheLogic;

        public TMyTenantController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IServiceProvider serviceProvider, IMapper mapper, IMasterDataRepository masterDataRepository, ITenantDataRepository tenantDataRepository, PlanCacheLogic planCacheLogic, TenantCacheLogic tenantCacheLogic, TrackCacheLogic trackCacheLogic) : base(logger)
        {
            this.settings = settings;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.mapper = mapper;
            this.masterDataRepository = masterDataRepository;
            this.tenantDataRepository = tenantDataRepository;
            this.planCacheLogic = planCacheLogic;
            this.tenantCacheLogic = tenantCacheLogic;
            this.trackCacheLogic = trackCacheLogic;
        }

        /// <summary>
        /// Get my tenant.
        /// </summary>
        /// <returns>Tenant.</returns>
        [ProducesResponseType(typeof(Api.TenantResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TenantResponse>> GetMyTenant()
        {
            try
            {
                var mTenant = await tenantDataRepository.GetTenantByNameAsync(RouteBinding.TenantName);

                await UpdatePaymentMandate(mTenant);

                return Ok(mapper.Map<Api.TenantResponse>(mTenant));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
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
        [ProducesResponseType(typeof(Api.TenantResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TenantResponse>> PutMyTenant([FromBody] Api.MyTenantRequest tenant)
        {
            try
            {
                var mTenant = await tenantDataRepository.GetTenantByNameAsync(RouteBinding.TenantName);

                var invalidateCustomDomainInCache = (!mTenant.CustomDomain.IsNullOrEmpty() && !mTenant.CustomDomain.Equals(tenant.CustomDomain, StringComparison.OrdinalIgnoreCase)) ? mTenant.CustomDomain : null;

                if (!RouteBinding.PlanName.IsNullOrEmpty() && !tenant.CustomDomain.IsNullOrEmpty())
                {
                    var plan = await planCacheLogic.GetPlanAsync(RouteBinding.PlanName);
                    if (!plan.EnableCustomDomain)
                    {
                        throw new Exception($"Custom domain is not supported in the '{plan.Name}' plan.");
                    }
                }

                try
                {
                    if (settings.Payment.EnablePayment == true && settings.Usage?.EnableInvoice == true && !tenant.PlanName.IsNullOrEmpty())
                    {
                        tenant.PlanName = tenant.PlanName.ToLower();
                        if (tenant.PlanName != RouteBinding.PlanName)
                        {
                            var mPlans = await masterDataRepository.GetListAsync<Plan>();
                            decimal currentCost = RouteBinding.PlanName.IsNullOrEmpty() ? 0 : mPlans.Where(p => p.Name == RouteBinding.PlanName).Select(p => p.CostPerMonth).FirstOrDefault();
                            decimal updateCost = mPlans.Where(p => p.Name == tenant.PlanName).Select(p => p.CostPerMonth).FirstOrDefault();
                            if (updateCost >= currentCost)
                            {
                                mTenant.PlanName = tenant.PlanName;
                            }
                        }
                    }
                }
                catch (Exception exp)
                {
                    logger.Error(exp, "Unable to update plan in tenant.");
                }

                if(!mTenant.CustomDomain.Equals(tenant.CustomDomain, StringComparison.OrdinalIgnoreCase))
                {
                    mTenant.CustomDomain = tenant.CustomDomain?.ToLower();
                    mTenant.CustomDomainVerified = false;
                }

                mTenant.Customer = mapper.Map<Customer>(tenant.Customer);
                await tenantDataRepository.UpdateAsync(mTenant);

                await tenantCacheLogic.InvalidateTenantCacheAsync(RouteBinding.TenantName);
                if (!invalidateCustomDomainInCache.IsNullOrEmpty())
                {
                    await tenantCacheLogic.InvalidateCustomDomainCacheAsync(invalidateCustomDomainInCache);
                }

                await UpdatePaymentMandate(mTenant);

                return Ok(mapper.Map<Api.TenantResponse>(mTenant));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update my '{typeof(Api.Tenant).Name}'.");
                    return NotFound(typeof(Api.Tenant).Name, RouteBinding.TenantName);
                }
                throw;
            }
        }

        private async Task UpdatePaymentMandate(Tenant mTenant)
        {
            if (settings.Payment?.EnablePayment == true && settings.Usage?.EnableInvoice == true)
            {
                var usageMolliePaymentLogic = serviceProvider.GetService<UsageMolliePaymentLogic>();
                await usageMolliePaymentLogic.UpdatePaymentMandate(mTenant);
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
     
                (var mTracks, _) = await tenantDataRepository.GetListAsync<Track>(new Track.IdKey { TenantName = RouteBinding.TenantName }, whereQuery: p => p.DataType.Equals("track"));
                foreach(var mTrack in mTracks)
                {
                    var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = mTrack.Name };
                    await tenantDataRepository.DeleteListAsync<DefaultElement>(trackIdKey);
                    await tenantDataRepository.DeleteAsync<Track>(mTrack.Id);

                    if (settings.Options.KeyStorage == KeyStorageOptions.KeyVault && !mTrack.Key.ExternalName.IsNullOrWhiteSpace())
                    {
                        await serviceProvider.GetService<ExternalKeyLogic>().DeleteExternalKeyAsync(mTrack.Key.ExternalName);
                    }
                    await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);
                }
                var mTenant = await tenantDataRepository.GetAsync<Tenant>(await Tenant.IdFormatAsync(RouteBinding.TenantName));
                await tenantDataRepository.DeleteAsync<Tenant>(await Tenant.IdFormatAsync(RouteBinding.TenantName));

                await tenantCacheLogic.InvalidateTenantCacheAsync(RouteBinding.TenantName);
                if (!string.IsNullOrEmpty(mTenant?.CustomDomain))
                {
                    await tenantCacheLogic.InvalidateCustomDomainCacheAsync(mTenant.CustomDomain);
                }

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete my '{typeof(Api.Tenant).Name}'.");
                    return NotFound(typeof(Api.Tenant).Name, RouteBinding.TenantName);
                }
                throw;
            }
        }
    }
}
