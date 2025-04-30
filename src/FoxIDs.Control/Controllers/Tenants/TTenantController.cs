using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Logic;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Infrastructure.Filters;
using System;
using ITfoxtec.Identity;
using FoxIDs.Models.Config;
using Microsoft.Extensions.DependencyInjection;
using FoxIDs.Logic.Usage;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]
    public class TTenantController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly IMasterDataRepository masterDataRepository;
        private readonly MasterTenantLogic masterTenantLogic;
        private readonly TenantCacheLogic tenantCacheLogic;
        private readonly TrackCacheLogic trackCacheLogic;

        public TTenantController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IServiceProvider serviceProvider, IMapper mapper, ITenantDataRepository tenantDataRepository, IMasterDataRepository masterDataRepository, MasterTenantLogic masterTenantLogic, TenantCacheLogic tenantCacheLogic, TrackCacheLogic trackCacheLogic) : base(logger)
        {
            this.settings = settings;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.masterDataRepository = masterDataRepository;
            this.masterTenantLogic = masterTenantLogic;
            this.tenantCacheLogic = tenantCacheLogic;
            this.trackCacheLogic = trackCacheLogic;
        }

        /// <summary>
        /// Get tenant.
        /// </summary>
        /// <param name="name">Tenant name.</param>
        /// <returns>Tenant.</returns>
        [ProducesResponseType(typeof(Api.TenantResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TenantResponse>> GetTenant(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);
                name = name?.ToLower();

                var mTenant = await tenantDataRepository.GetTenantByNameAsync(name);
                if (mTenant.ForUsage != false)
                {
                    await UpdatePaymentMandate(mTenant);
                }
                return Ok(mapper.Map<Api.TenantResponse>(mTenant));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.Tenant).Name}' by name '{name}'.");
                    return NotFound(typeof(Api.Tenant).Name, name);
                }
                throw;
            }
        }

        /// <summary>
        /// Create tenant.
        /// </summary>
        /// <param name="tenant">Tenant.</param>
        /// <returns>Tenant.</returns>
        [ProducesResponseType(typeof(Api.TenantResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.TenantResponse>> PostTenant([FromBody] Api.CreateTenantRequest tenant)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(tenant)) return BadRequest(ModelState);
                tenant.Name = tenant.Name.ToLower();

                if (!tenant.ForUsage)
                {
                    tenant.AdministratorEmail = tenant.AdministratorEmail?.ToLower();

                    if (tenant.Name == Constants.Routes.ControlSiteName || tenant.Name == Constants.Routes.HealthController)
                    {
                        throw new FoxIDsDataException($"A tenant can not have the name '{tenant.Name}'.") { StatusCode = DataStatusCode.Conflict };
                    }
                }

                var mTenant = mapper.Map<Tenant>(tenant);

                mTenant.CustomDomain = tenant.CustomDomain?.ToLower();
                mTenant.CustomDomainVerified = !string.IsNullOrWhiteSpace(tenant.CustomDomain) && tenant.CustomDomainVerified;

                mTenant.Customer = mapper.Map<Customer>(tenant.Customer);
                mTenant.CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                if (!tenant.ForUsage)
                {
                    (var validPlan, var plan) = await ValidatePlanAsync(tenant.Name, nameof(tenant.PlanName), tenant.PlanName);
                    if (!validPlan) return BadRequest(ModelState);

                    if (plan != null && !tenant.CustomDomain.IsNullOrEmpty())
                    {
                        if (!plan.EnableCustomDomain)
                        {
                            throw new Exception($"Custom domain is not supported in the '{plan.Name}' plan.");
                        }
                    }
                    mTenant.PlanName = plan?.Name;
                }

                await tenantDataRepository.CreateAsync(mTenant);

                if (!tenant.ForUsage)
                {
                    await tenantCacheLogic.InvalidateTenantCacheAsync(tenant.Name);
                    if (!string.IsNullOrEmpty(tenant.CustomDomain))
                    {
                        await tenantCacheLogic.InvalidateCustomDomainCacheAsync(tenant.CustomDomain);
                    }

                    await masterTenantLogic.CreateMasterTrackDocumentAsync(tenant.Name);
                    var mLoginUpParty = await masterTenantLogic.CreateMasterLoginDocumentAsync(tenant.Name);
                    await masterTenantLogic.CreateFirstAdminUserDocumentAsync(tenant.Name, tenant.AdministratorEmail, tenant.AdministratorPassword, tenant.ChangeAdministratorPassword, true, tenant.ConfirmAdministratorAccount);
                    await masterTenantLogic.CreateMasterFoxIDsControlApiResourceDocumentAsync(tenant.Name);
                    await masterTenantLogic.CreateMasterControlClientDocmentAsync(tenant.Name, tenant.ControlClientBaseUri, mLoginUpParty);

                    if (tenant.ChangeAdministratorPassword)
                    {
                        await masterTenantLogic.CreateDefaultTracksDocmentsAsync(tenant.Name);
                    }
                    else
                    {
                        await masterTenantLogic.CreateDefaultTracksDocmentsAsync(tenant.Name, tenant.AdministratorEmail, tenant.AdministratorPassword);
                    }
                }
                return Created(mapper.Map<Api.TenantResponse>(mTenant));
            }
            catch (AccountException aex)
            {
                try
                {
                    await DeleteTenant(tenant.Name);
                }
                catch (Exception delEx)
                {
                    logger.Warning(delEx, "Create tenant delete, try to delete incorrectly created tenant.");
                }
                ModelState.TryAddModelError(nameof(tenant.AdministratorPassword), aex.Message);
                return BadRequest(ModelState, aex);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create '{typeof(Api.Tenant).Name}' by name '{tenant.Name}'.");
                    return Conflict(typeof(Api.Tenant).Name, tenant.Name, nameof(tenant.Name));
                }
                else
                {
                    try
                    {
                        await DeleteTenant(tenant.Name);
                    }
                    catch (Exception delEx)
                    {
                        logger.Warning(delEx, "Create tenant delete, try to delete incorrectly created tenant.");
                    }
                }
                throw;
            }
        }

        /// <summary>
        /// Update tenant.
        /// </summary>
        /// <param name="tenant">Tenant.</param>
        /// <returns>Tenant.</returns>
        [ProducesResponseType(typeof(Api.TenantResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TenantResponse>> PutTenant([FromBody] Api.TenantRequest tenant)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(tenant)) return BadRequest(ModelState);
                tenant.Name = tenant.Name.ToLower();

                var mTenant = await tenantDataRepository.GetTenantByNameAsync(tenant.Name);
                var invalidateCustomDomainInCache = (!mTenant.CustomDomain.IsNullOrEmpty() && !mTenant.CustomDomain.Equals(tenant.CustomDomain, StringComparison.OrdinalIgnoreCase)) ? mTenant.CustomDomain : null;

                if (!tenant.ForUsage)
                {
                    (var validPlan, var plan) = await ValidatePlanAsync(tenant.Name, nameof(tenant.PlanName), tenant.PlanName);
                    if (!validPlan) return BadRequest(ModelState);

                    mTenant.PlanName = plan?.Name;

                    if (plan != null && !tenant.CustomDomain.IsNullOrEmpty())
                    {
                        if (!plan.EnableCustomDomain)
                        {
                            throw new Exception($"Custom domain is not supported in the '{plan.Name}' plan.");
                        }
                    }
                    mTenant.CustomDomain = tenant.CustomDomain?.ToLower();
                    mTenant.CustomDomainVerified = !string.IsNullOrWhiteSpace(tenant.CustomDomain) && tenant.CustomDomainVerified;
                }
                mTenant.EnableUsage = tenant.EnableUsage;
                mTenant.DoPayment = tenant.DoPayment;
                mTenant.Currency = tenant.Currency;
                mTenant.IncludeVat = tenant.IncludeVat;
                mTenant.HourPrice = tenant.HourPrice;
                mTenant.Customer = mapper.Map<Customer>(tenant.Customer);
                await tenantDataRepository.UpdateAsync(mTenant);

                await tenantCacheLogic.InvalidateTenantCacheAsync(tenant.Name);
                if (!tenant.ForUsage)
                {
                    if (!invalidateCustomDomainInCache.IsNullOrEmpty())
                    {
                        await tenantCacheLogic.InvalidateCustomDomainCacheAsync(invalidateCustomDomainInCache);
                    }

                    await UpdatePaymentMandate(mTenant);
                }

                return Ok(mapper.Map<Api.TenantResponse>(mTenant));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.Tenant).Name}' by name '{tenant.Name}'.");
                    return NotFound(typeof(Api.Tenant).Name, tenant.Name, nameof(tenant.Name));
                }
                throw;
            }
        }

        private async Task<(bool, Plan)> ValidatePlanAsync(string tenantName, string propertyName, string planName)
        {
            if(planName.IsNullOrWhiteSpace())
            {
                return (true, null);
            }

            try
            {
                var plan = await masterDataRepository.GetAsync<Plan>(await Plan.IdFormatAsync(planName));
                return (true, plan);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    var errorMessage = $"Plan '{planName}' not found and can not be connected to tenant '{tenantName}'.";
                    logger.Warning(ex, errorMessage);
                    ModelState.TryAddModelError(propertyName.ToCamelCase(), errorMessage);
                    return (false, null);
                }
                else
                {
                    throw;
                }
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
        /// Delete tenant.
        /// </summary>
        /// <param name="name">Tenant name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTenant(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);
                name = name?.ToLower();

                if (name.Equals(Constants.Routes.MasterTenantName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("The master tenant can not be deleted.");
                }
     
                (var mTracks, _) = await tenantDataRepository.GetListAsync<Track>(new Track.IdKey { TenantName = name }, whereQuery: p => p.DataType.Equals("track"));
                foreach(var mTrack in mTracks)
                {
                    var trackIdKey = new Track.IdKey { TenantName = name, TrackName = mTrack.Name };
                    await tenantDataRepository.DeleteListAsync<DefaultElement>(trackIdKey);
                    await tenantDataRepository.DeleteAsync<Track>(mTrack.Id);

                    if (settings.Options.KeyStorage == KeyStorageOptions.KeyVault && !mTrack.Key.ExternalName.IsNullOrWhiteSpace())
                    {
                        await serviceProvider.GetService<ExternalKeyLogic>().DeleteExternalKeyAsync(mTrack.Key.ExternalName);
                    }
                    await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);
                }
                var mTenant = await tenantDataRepository.GetAsync<Tenant>(await Tenant.IdFormatAsync(name));
                await tenantDataRepository.DeleteAsync<Tenant>(await Tenant.IdFormatAsync(name));

                await tenantCacheLogic.InvalidateTenantCacheAsync(name);
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
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.Tenant).Name}' by name '{name}'.");
                    return NotFound(typeof(Api.Tenant).Name, name);
                }
                throw;
            }
        }
    }
}
