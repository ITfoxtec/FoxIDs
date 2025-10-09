using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Repository;
using FoxIDs.Models;
using AutoMapper;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Basic, Constants.ControlApi.Segment.Party)]
    public class TPlanInfoController : ApiController
    {
        private readonly IMapper mapper;
        private readonly IMasterDataRepository masterDataRepository;

        public TPlanInfoController(TelemetryScopedLogger logger, IMapper mapper, IMasterDataRepository masterDataRepository) : base(logger, auditLogEnabled: false)
        {
            this.mapper = mapper;
            this.masterDataRepository = masterDataRepository;
        }

        /// <summary>
        /// Get list of plans.
        /// </summary>
        /// <returns>Client settings.</returns>
        [ProducesResponseType(typeof(HashSet<Api.PlanInfo>), StatusCodes.Status200OK)]
        public async Task<ActionResult<HashSet<Api.PlanInfo>>> GetPlanInfo()
        {
            try
            {
                var mPlans = await masterDataRepository.GetManyAsync<Plan>();
                var aPlans = new HashSet<Api.PlanInfo>(mPlans.Count());
                foreach (var mPlan in mPlans.OrderBy(p => p.CostPerMonth).ThenBy(t => t.Name))
                {
                    aPlans.Add(mapper.Map<Api.PlanInfo>(mPlan));
                }
                return Ok(aPlans);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    return Ok(new HashSet<Api.PlanInfo>());
                }
                throw;
            }
        }
    }
}
