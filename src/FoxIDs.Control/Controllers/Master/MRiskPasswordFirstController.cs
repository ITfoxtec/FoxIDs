using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]
    public class MRiskPasswordFirstController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly IMasterDataRepository masterDataRepository;

        public MRiskPasswordFirstController(TelemetryScopedLogger logger, IMapper mapper, IMasterDataRepository masterDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.masterDataRepository = masterDataRepository;
        }

        /// <summary>
        /// Get the first 1000 risk password. Can be used query risk passwords before deleting them.
        /// </summary>
        /// <returns>Risk passwords.</returns>
        [ProducesResponseType(typeof(HashSet<Api.RiskPassword>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HashSet<Api.RiskPassword>>> GetRiskPasswordFirst()
        {
            var mRiskPasswords = await masterDataRepository.GetListAsync<RiskPassword>(pageSize: Constants.Models.ListPageSize);
            if (mRiskPasswords?.Count > 0) 
            {
                return Ok(mapper.Map<HashSet<Api.RiskPassword>>(mRiskPasswords));
            }
            else
            {
                return Ok();
            }
        }
    }
}
