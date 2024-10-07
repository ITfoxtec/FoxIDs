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
using Mollie.Api.Models.Mandate.Response.PaymentSpecificParameters;
using Mollie.Api.Client.Abstract;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TMyTenantController :  ApiController
    {
        private readonly Settings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly PlanCacheLogic planCacheLogic;
        private readonly TenantCacheLogic tenantCacheLogic;
        private readonly TrackCacheLogic trackCacheLogic;
        private readonly IMandateClient mandateClient;

        public TMyTenantController(Settings settings, TelemetryScopedLogger logger, IServiceProvider serviceProvider, IMapper mapper, ITenantDataRepository tenantDataRepository, PlanCacheLogic planCacheLogic, TenantCacheLogic tenantCacheLogic, TrackCacheLogic trackCacheLogic, IMandateClient mandateClient) : base(logger)
        {
            this.settings = settings;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.planCacheLogic = planCacheLogic;
            this.tenantCacheLogic = tenantCacheLogic;
            this.trackCacheLogic = trackCacheLogic;
            this.mandateClient = mandateClient;
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
                var mTenant = await tenantDataRepository.GetTenantByNameAsync(RouteBinding.TenantName);

                await UpdatePayment(mTenant);

                return Ok(mapper.Map<Api.Tenant>(mTenant));
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
        [ProducesResponseType(typeof(Api.Tenant), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.Tenant>> PutMyTenant([FromBody] Api.MyTenantRequest tenant)
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

                mTenant.CustomDomain = tenant.CustomDomain;
                mTenant.CustomDomainVerified = false;
                await tenantDataRepository.UpdateAsync(mTenant);

                await tenantCacheLogic.InvalidateTenantCacheAsync(RouteBinding.TenantName);
                if (!invalidateCustomDomainInCache.IsNullOrEmpty())
                {
                    await tenantCacheLogic.InvalidateCustomDomainCacheAsync(invalidateCustomDomainInCache);
                }

                await UpdatePayment(mTenant);

                return Ok(mapper.Map<Api.Tenant>(mTenant));
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

        private async Task UpdatePayment(Tenant mTenant)
        {
            if (mTenant.Payment != null && string.IsNullOrEmpty(mTenant.Payment.CardNumber) && !string.IsNullOrEmpty(mTenant.Payment.MandateId))
            {
                var mandateResponse = await mandateClient.GetMandateAsync(mTenant.Payment.CustomerId, mTenant.Payment.MandateId) as CreditCardMandateResponse;
                if ("valid".Equals(mandateResponse.Status, StringComparison.OrdinalIgnoreCase))
                {
                    mTenant.Payment.IsActive = true;
                    var cardExpiryDate = DateTime.Parse(mandateResponse.Details.CardExpiryDate);
                    mTenant.Payment.CardHolder = mandateResponse.Details.CardHolder;
                    mTenant.Payment.CardNumber = mandateResponse.Details.CardNumber;
                    mTenant.Payment.CardLabel = mandateResponse.Details.CardLabel;
                    mTenant.Payment.CardExpiryMonth = cardExpiryDate.Month;
                    mTenant.Payment.CardExpiryYear = cardExpiryDate.Year;

                    await tenantDataRepository.UpdateAsync(mTenant);
                }
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
