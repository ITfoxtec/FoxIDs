using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using ITfoxtec.Identity;
using System.Linq;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Infrastructure.Security;
using System;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]

    public class MFilterPlanController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly IMasterDataRepository masterDataRepository;

        public MFilterPlanController(TelemetryScopedLogger logger, IMapper mapper, IMasterDataRepository masterDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.masterDataRepository = masterDataRepository;
        }


        /// <summary>
        /// Filter plan.
        /// </summary>
        /// <param name="filterName">Filter plan name.</param>
        /// <returns>Plans.</returns>
        [ProducesResponseType(typeof(HashSet<Api.Plan>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HashSet<Api.Plan>>> GetFilterPlan(string filterName)
        {
            try
            {
                var mPlans = await GetFilterPlanInternalAsync(filterName);
                var aPlans = new HashSet<Api.Plan>(mPlans.Count());
                foreach (var mPlan in mPlans.OrderBy(p => p.CostPerMonth).ThenBy(t => t.Name))
                {
                    aPlans.Add(mapper.Map<Api.Plan>(mPlan));
                }
                return Ok(aPlans);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.Plan).Name}' by filter name '{filterName}'.");
                    return NotFound(typeof(Api.Plan).Name, filterName);
                }
                throw;
            }
        }

        private ValueTask<IReadOnlyCollection<Plan>> GetFilterPlanInternalAsync(string filterName)
        {
            if (filterName.IsNullOrWhiteSpace())
            {
                return masterDataRepository.GetListAsync<Plan>();
            }
            else
            {
                return masterDataRepository.GetListAsync<Plan>(whereQuery: t => t.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase));
            }
        }
    }
}
