using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AutoMapper;

namespace FoxIDs.Controllers
{
    public class MRiskPasswordInfoController : MasterApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly IMasterRepository masterRepository;

        public MRiskPasswordInfoController(TelemetryScopedLogger logger, IMapper mapper, IMasterRepository masterRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.masterRepository = masterRepository;
        }

        /// <summary>
        /// Get risk password info.
        /// </summary>
        /// <returns>Risk password info.</returns>
        [ProducesResponseType(typeof(Api.RiskPasswordInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.RiskPasswordInfo>> GetRiskPasswordInfo()
        {
            var mRiskPasswordCount = await masterRepository.CountAsync<RiskPassword>();
            return Ok(new Api.RiskPasswordInfo { RiskPasswordCount = mRiskPasswordCount });
        }
    }
}
