using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    public class MRiskPasswordInfoController : MasterApiController
    {
        private readonly IMasterRepository masterRepository;

        public MRiskPasswordInfoController(TelemetryScopedLogger logger, IMasterRepository masterRepository) : base(logger)
        {
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
