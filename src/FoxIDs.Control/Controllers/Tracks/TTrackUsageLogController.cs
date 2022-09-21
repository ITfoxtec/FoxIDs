using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Models.Config;
using FoxIDs.Logic;

namespace FoxIDs.Controllers
{
    public class TTrackUsageLogController : TenantApiController
    {
        private readonly UsageLogLogic usageLogLogic;

        public TTrackUsageLogController(TelemetryScopedLogger logger, UsageLogLogic usageLogLogic) : base(logger)
        {
            this.usageLogLogic = usageLogLogic;
        }

        /// <summary>
        /// Get track usage logs.
        /// </summary>
        /// <returns>Logs.</returns>
        [ProducesResponseType(typeof(Api.UsageLogResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.UsageLogResponse>> GetTrackUsageLog(Api.UsageLogRequest logRequest)
        {
            if (!await ModelState.TryValidateObjectAsync(logRequest)) return BadRequest(ModelState);

            var logResponse = await usageLogLogic.GetTrackUsageLog(logRequest);
            return Ok(logResponse);
        }
    }
}
