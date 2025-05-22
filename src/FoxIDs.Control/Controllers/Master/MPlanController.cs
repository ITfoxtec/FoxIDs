using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AutoMapper;
using System.Linq;
using System;
using FoxIDs.Logic;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]
    public class MPlanController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly IMasterDataRepository masterDataRepository;
        private readonly PlanCacheLogic planCacheLogic;

        public MPlanController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, IMasterDataRepository masterDataRepository, PlanCacheLogic planCacheLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.masterDataRepository = masterDataRepository;
            this.planCacheLogic = planCacheLogic;
        }

        /// <summary>
        /// Get plan.
        /// </summary>
        /// <param name="name">Plan name.</param>
        /// <returns>Plan.</returns>
        [ProducesResponseType(typeof(Api.Plan), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.Plan>> GetPlan(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);
                name = name?.ToLower();

                var mPlan = await masterDataRepository.GetAsync<Plan>(await Plan.IdFormatAsync(name));

                return Ok(mapper.Map<Api.Plan>(mPlan));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.Plan).Name}' by name '{name}'.");
                    return NotFound(typeof(Api.Plan).Name, name);
                }
                throw;
            }
        }

        /// <summary>
        /// Create plan.
        /// </summary>
        /// <param name="plan">plan.</param>
        /// <returns>plan.</returns>
        [ProducesResponseType(typeof(Api.Plan), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.Plan>> PostPlan([FromBody] Api.Plan plan)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(plan)) return BadRequest(ModelState);
                plan.Name = plan.Name.ToLower();

                var count = await masterDataRepository.CountAsync<Plan>();
                if (count >= Constants.Models.Plan.PlansMax)
                {
                    throw new Exception($"Maximum number of plans ({Constants.Models.Plan.PlansMax}) has been reached.");
                }

                var mPlan = mapper.Map<Plan>(plan);
                await masterDataRepository.CreateAsync(mPlan);

                return Created(mapper.Map<Api.Plan>(mPlan));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create '{typeof(Api.Plan).Name}' by name '{plan.Name}'.");
                    return Conflict(typeof(Api.Plan).Name, plan.Name, nameof(plan.Name));
                }
                throw;
            }
        }

        /// <summary>
        /// Update plan.
        /// </summary>
        /// <param name="plan">Plan.</param>
        /// <returns>Plan.</returns>
        [ProducesResponseType(typeof(Api.Plan), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.Plan>> PutPlan([FromBody] Api.Plan plan)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(plan)) return BadRequest(ModelState);
                plan.Name = plan.Name.ToLower();

                var mPlan = mapper.Map<Plan>(plan);
                await masterDataRepository.UpdateAsync(mPlan);

                await planCacheLogic.InvalidatePlanCacheAsync(plan.Name);

                return Ok(mapper.Map<Api.Plan>(mPlan));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.Plan).Name}' by name '{plan.Name}'.");
                    return NotFound(typeof(Api.Plan).Name, plan.Name, nameof(plan.Name));
                }
                throw;
            }
        }

        /// <summary>
        /// Delete plan.
        /// </summary>
        /// <param name="name">Plan name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePlan(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);
                name = name?.ToLower();

                if (!await ValidatePlanNotUsedAsync(name)) return BadRequest(ModelState);

                await masterDataRepository.DeleteAsync<Plan>(await Plan.IdFormatAsync(name));

                await planCacheLogic.InvalidatePlanCacheAsync(name);

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.Plan).Name}' by name '{name}'.");
                    return NotFound(typeof(Api.Plan).Name, name);
                }
                throw;
            }
        }

        private async Task<bool> ValidatePlanNotUsedAsync(string planName)
        {
            (var tenants, _) = await tenantDataRepository.GetManyAsync<Tenant>(whereQuery: t => t.DataType.Equals(Constants.Models.DataType.Tenant) && t.PlanName.Equals(planName), pageSize: 1);
            if (tenants.Count() > 0)
            {
                try
                {
                    throw new Exception($"Plan is used by tenant '{tenants.First().Name}' and can not be deleted.");
                }
                catch (Exception ex)
                {
                    logger.Warning(ex);
                    ModelState.TryAddModelError(string.Empty, ex.Message);
                    return false;
                }                                 
            }
            return true;
        }
    }
}
