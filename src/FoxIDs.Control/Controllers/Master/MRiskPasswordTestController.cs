using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    public class MRiskPasswordTestController : MasterApiController
    {
        private readonly IMasterRepository masterRepository;

        public MRiskPasswordTestController(TelemetryScopedLogger logger, IMasterRepository masterRepository) : base(logger)
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
                var mRiskPassword = await masterRepository.GetAsync<RiskPassword>(await RiskPassword.IdFormat(passwordSha1Hash));
                return Ok(true);
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return Ok(false);
                }
                throw;
            }
        }
    }
}
