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
        private readonly IMasterDataRepository masterDataRepository;

        public MRiskPasswordInfoController(TelemetryScopedLogger logger, IMasterDataRepository masterDataRepository) : base(logger)
        {
            this.masterDataRepository = masterDataRepository;
        }

        /// <summary>
        /// Get risk password info.
        /// </summary>
        /// <returns>Risk password info.</returns>
        [ProducesResponseType(typeof(Api.RiskPasswordInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.RiskPasswordInfo>> GetRiskPasswordInfo()
        {
            var mRiskPasswordCount = await masterDataRepository.CountAsync<RiskPassword>();
            return Ok(new Api.RiskPasswordInfo { RiskPasswordCount = mRiskPasswordCount });
        }
    }
}
