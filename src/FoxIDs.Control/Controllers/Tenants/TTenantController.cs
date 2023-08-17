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
using FoxIDs.Infrastructure.Security;
using FoxIDs.Infrastructure.Filters;
using System;
using ITfoxtec.Identity;


namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]
    public class TTenantController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;
        private readonly IMasterRepository masterRepository;
        private readonly MasterTenantLogic masterTenantLogic;
        private readonly PlanCacheLogic planCacheLogic;
        private readonly TenantCacheLogic tenantCacheLogic;
        private readonly TrackCacheLogic trackCacheLogic;
        private readonly ExternalKeyLogic externalKeyLogic;

        public TTenantController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, IMasterRepository masterRepository, MasterTenantLogic masterTenantLogic, PlanCacheLogic planCacheLogic, TenantCacheLogic tenantCacheLogic, TrackCacheLogic trackCacheLogic, ExternalKeyLogic externalKeyLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
            this.masterRepository = masterRepository;
            this.masterTenantLogic = masterTenantLogic;
            this.planCacheLogic = planCacheLogic;
            this.tenantCacheLogic = tenantCacheLogic;
            this.trackCacheLogic = trackCacheLogic;
            this.externalKeyLogic = externalKeyLogic;
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

                var MTenant = await tenantRepository.GetTenantByNameAsync(name);
                return Ok(mapper.Map<Api.Tenant>(MTenant));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
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

                (var validPlan, var plan) = await ValidatePlanAsync(tenant.Name, nameof(tenant.PlanName), tenant.PlanName);
                if (!validPlan) return BadRequest(ModelState);

                if (plan != null && !tenant.CustomDomain.IsNullOrEmpty())
                {
                    if (!plan.EnableCustomDomain)
                    {
                        throw new Exception($"Custom domain not enabled by plan '{plan.Name}'.");
                    }
                }

                var mTenant = mapper.Map<Tenant>(tenant);
                await tenantRepository.CreateAsync(mTenant);

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

                await CreateTrackDocumentAsync(tenant.Name, "test");
                await CreateTrackDocumentAsync(tenant.Name, "-"); // production

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
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Conflict)
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

        private async Task CreateTrackDocumentAsync(string tenantName, string trackName)
        {
            var mTrack = mapper.Map<Track>(new Api.Track { Name = trackName?.ToLower() });
            await masterTenantLogic.CreateTrackDocumentAsync(tenantName, mTrack);
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

                var mTenant = await tenantRepository.GetTenantByNameAsync(tenant.Name);

                var invalidateCustomDomainInCache = (!mTenant.CustomDomain.IsNullOrEmpty() && !mTenant.CustomDomain.Equals(tenant.CustomDomain, StringComparison.OrdinalIgnoreCase)) ? mTenant.CustomDomain : null;

                (var validPlan, var plan) = await ValidatePlanAsync(tenant.Name, nameof(tenant.PlanName), tenant.PlanName);
                if (!validPlan) return BadRequest(ModelState);

                mTenant.PlanName = tenant.PlanName;
                
                if (plan != null && !tenant.CustomDomain.IsNullOrEmpty())
                {
                    if (!plan.EnableCustomDomain)
                    {
                        throw new Exception($"Custom domain not enabled by plan '{plan.Name}'.");
                    }
                }
                mTenant.CustomDomain = tenant.CustomDomain;
                mTenant.CustomDomainVerified = tenant.CustomDomainVerified;
                await tenantRepository.UpdateAsync(mTenant);

                await tenantCacheLogic.InvalidateTenantCacheAsync(tenant.Name);
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
                var plan = await masterRepository.GetAsync<Plan>(await Plan.IdFormatAsync(planName));
                return (true, plan);
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
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
     
                (var mTracks, _) = await tenantRepository.GetListAsync<Track>(new Track.IdKey { TenantName = name }, whereQuery: p => p.DataType.Equals("track"));
                foreach(var mTrack in mTracks)
                {
                    var trackIdKey = new Track.IdKey { TenantName = name, TrackName = mTrack.Name };
                    await tenantRepository.DeleteListAsync<DefaultElement>(trackIdKey);
                    await tenantRepository.DeleteAsync<Track>(mTrack.Id);

                    if (mTrack.Key.Type == TrackKeyTypes.KeyVaultRenewSelfSigned)
                    {
                        await externalKeyLogic.DeleteExternalKeyAsync(mTrack.Key.ExternalName);
                    }
                    await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);
                }
                var mTenant = await tenantRepository.DeleteAsync<Tenant>(await Tenant.IdFormatAsync(name));

                await tenantCacheLogic.InvalidateTenantCacheAsync(name);
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
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.Tenant).Name}' by name '{name}'.");
                    return NotFound(typeof(Api.Tenant).Name, name);
                }
                throw;
            }
        }
    }
}
