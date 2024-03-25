using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]
    public class MRiskPasswordInfoController : ApiController
    {
        private readonly IMasterDataRepository masterRepository;

        public MRiskPasswordInfoController(TelemetryScopedLogger logger, IMasterDataRepository masterRepository) : base(logger)
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
