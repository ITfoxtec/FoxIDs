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

    public class MPlansController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly IMasterDataRepository masterDataRepository;

        public MPlansController(TelemetryScopedLogger logger, IMapper mapper, IMasterDataRepository masterDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.masterDataRepository = masterDataRepository;
        }


        /// <summary>
        /// Get plans.
        /// </summary>
        /// <param name="filterName">Filter plan name.</param>
        /// <param name="paginationToken">The pagination token.</param>
        /// <returns>Plans.</returns>
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.Plan>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.PaginationResponse<Api.Plan>>> GetPlans(string filterName, string paginationToken = null)
        {
            try
            {
                var mPlans = await GetFilterPlanInternalAsync(filterName);

                var response = new Api.PaginationResponse<Api.Plan>
                {
                    Data = new HashSet<Api.Plan>(mPlans.Count()),
                };
                foreach (var mPlan in mPlans.OrderBy(p => p.CostPerMonth).ThenBy(t => t.Name))
                {
                    response.Data.Add(mapper.Map<Api.Plan>(mPlan));
                }
                return Ok(response);
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
                return masterDataRepository.GetManyAsync<Plan>();
            }
            else
            {
                return masterDataRepository.GetManyAsync<Plan>(whereQuery: t => t.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase));
            }
        }
    }
}
