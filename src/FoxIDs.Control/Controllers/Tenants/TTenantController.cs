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
        [ProducesResponseType(typeof(Api.Tenant), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.Tenant>> GetTenant(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);
                name = name?.ToLower();

                var MTenant = await tenantDataRepository.GetTenantByNameAsync(name);
                return Ok(mapper.Map<Api.Tenant>(MTenant));
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
        [ProducesResponseType(typeof(Api.Tenant), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.Tenant>> PostTenant([FromBody] Api.CreateTenantRequest tenant)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(tenant)) return BadRequest(ModelState);
                tenant.Name = tenant.Name.ToLower();
                tenant.AdministratorEmail = tenant.AdministratorEmail?.ToLower();

                if (tenant.Name == Constants.Routes.ControlSiteName)
                {
                    throw new FoxIDsDataException($"A tenant can not have the name '{Constants.Routes.ControlSiteName}'.") { StatusCode = DataStatusCode.Conflict };
                }

                (var validPlan, var plan) = await ValidatePlanAsync(tenant.Name, nameof(tenant.PlanName), tenant.PlanName);
                if (!validPlan) return BadRequest(ModelState);

                if (plan != null && !tenant.CustomDomain.IsNullOrEmpty())
                {
                    if (!plan.EnableCustomDomain)
                    {
                        throw new Exception($"Custom domain is not supported in the '{plan.Name}' plan.");
                    }
                }

                var mTenant = mapper.Map<Tenant>(tenant);
                await tenantDataRepository.CreateAsync(mTenant);

                await tenantCacheLogic.InvalidateTenantCacheAsync(tenant.Name);
                if (!string.IsNullOrEmpty(tenant.CustomDomain))
                {
                    await tenantCacheLogic.InvalidateCustomDomainCacheAsync(tenant.CustomDomain);
                }

                await masterTenantLogic.CreateMasterTrackDocumentAsync(tenant.Name, plan.GetKeyType(settings.Options.KeyStorage == KeyStorageOptions.KeyVault));
                var mLoginUpParty = await masterTenantLogic.CreateMasterLoginDocumentAsync(tenant.Name);
                await masterTenantLogic.CreateFirstAdminUserDocumentAsync(tenant.Name, tenant.AdministratorEmail, tenant.AdministratorPassword, tenant.ChangeAdministratorPassword, true, tenant.ConfirmAdministratorAccount);
                await masterTenantLogic.CreateMasterFoxIDsControlApiResourceDocumentAsync(tenant.Name);
                await masterTenantLogic.CreateMasterControlClientDocmentAsync(tenant.Name, tenant.ControlClientBaseUri, mLoginUpParty);

                await CreateTrackDocumentAsync(tenant.Name, "Test", "test", plan.GetKeyType(settings.Options.KeyStorage == KeyStorageOptions.KeyVault));
                await CreateTrackDocumentAsync(tenant.Name, "Production", "-", plan.GetKeyType(settings.Options.KeyStorage == KeyStorageOptions.KeyVault));

                return Created(mapper.Map<Api.Tenant>(mTenant));
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

        private async Task CreateTrackDocumentAsync(string tenantName, string trackDisplayName, string trackName, TrackKeyTypes keyType)
        {
            var mTrack = mapper.Map<Track>(new Api.Track { DisplayName = trackDisplayName, Name = trackName?.ToLower() });
            await masterTenantLogic.CreateTrackDocumentAsync(tenantName, mTrack, keyType);
            await masterTenantLogic.CreateLoginDocumentAsync(tenantName, mTrack.Name);
        }

        /// <summary>
        /// Update tenant.
        /// </summary>
        /// <param name="tenant">Tenant.</param>
        /// <returns>Tenant.</returns>
        [ProducesResponseType(typeof(Api.Tenant), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.Tenant>> PutTenant([FromBody] Api.TenantRequest tenant)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(tenant)) return BadRequest(ModelState);
                tenant.Name = tenant.Name.ToLower();

                var mTenant = await tenantDataRepository.GetTenantByNameAsync(tenant.Name);

                var invalidateCustomDomainInCache = (!mTenant.CustomDomain.IsNullOrEmpty() && !mTenant.CustomDomain.Equals(tenant.CustomDomain, StringComparison.OrdinalIgnoreCase)) ? mTenant.CustomDomain : null;

                (var validPlan, var plan) = await ValidatePlanAsync(tenant.Name, nameof(tenant.PlanName), tenant.PlanName);
                if (!validPlan) return BadRequest(ModelState);

                mTenant.PlanName = tenant.PlanName;
                
                if (plan != null && !tenant.CustomDomain.IsNullOrEmpty())
                {
                    if (!plan.EnableCustomDomain)
                    {
                        throw new Exception($"Custom domain is not supported in the '{plan.Name}' plan.");
                    }
                }
                mTenant.CustomDomain = tenant.CustomDomain;
                mTenant.CustomDomainVerified = tenant.CustomDomainVerified;
                await tenantDataRepository.UpdateAsync(mTenant);

                await tenantCacheLogic.InvalidateTenantCacheAsync(tenant.Name);
                if (!invalidateCustomDomainInCache.IsNullOrEmpty())
                {
                    await tenantCacheLogic.InvalidateCustomDomainCacheAsync(invalidateCustomDomainInCache);
                }

                return Ok(mapper.Map<Api.Tenant>(mTenant));
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
