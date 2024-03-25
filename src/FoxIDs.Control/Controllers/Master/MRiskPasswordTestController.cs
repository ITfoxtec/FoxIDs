using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]
    public class MRiskPasswordTestController : ApiController
    {
        private readonly IMasterDataRepository masterRepository;

        public MRiskPasswordTestController(TelemetryScopedLogger logger, IMasterDataRepository masterRepository) : base(logger)
        {
            this.masterRepository = masterRepository;
        }

        /// <summary>
        /// Test if a password has appeared in breaches and is in risk.
        /// </summary>
        /// <param name="password">Password.</param>
        /// <returns>True if in risk.</returns>
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<bool>> GetRiskPasswordTest(string password)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(password, nameof(password))) return BadRequest(ModelState);

                var passwordSha1Hash = password.Sha1Hash();
                var mRiskPassword = await masterRepository.GetAsync<RiskPassword>(await RiskPassword.IdFormatAsync(passwordSha1Hash));
                return Ok(true);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    return Ok(false);
                }
                throw;
            }
        }
    }
}
