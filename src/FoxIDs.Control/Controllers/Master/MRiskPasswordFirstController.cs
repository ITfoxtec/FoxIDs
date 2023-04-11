using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;

namespace FoxIDs.Controllers
{
    public class MRiskPasswordFirstController : MasterApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly IMasterRepository masterRepository;

        public MRiskPasswordFirstController(TelemetryScopedLogger logger, IMapper mapper, IMasterRepository masterRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.masterRepository = masterRepository;
        }

        /// <summary>
        /// Get the first 1000 risk password. Can be used query risk passwords before deleting them.
        /// </summary>
        /// <returns>Risk passwords.</returns>
        [ProducesResponseType(typeof(HashSet<Api.RiskPassword>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HashSet<Api.RiskPassword>>> GetRiskPasswordFirst()
        {
            var mRiskPasswords = await masterRepository.GetListAsync<RiskPassword>(maxItemCount: 1000);
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
