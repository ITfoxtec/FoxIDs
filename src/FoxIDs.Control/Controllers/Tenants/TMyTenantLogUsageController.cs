using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Logic;
using ITfoxtec.Identity;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Log)]
    public class TMyTenantLogUsageController : ApiController
    {
        private readonly UsageLogLogic usageLogLogic;

        public TMyTenantLogUsageController(TelemetryScopedLogger logger, UsageLogLogic usageLogLogic) : base(logger)
        {
            this.usageLogLogic = usageLogLogic;
        }

        /// <summary>
        /// Get my tenant usage logs.
        /// </summary>
        /// <returns>Logs.</returns>
        [ProducesResponseType(typeof(Api.UsageLogResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.UsageLogResponse>> GetMyTenantLogUsage(Api.UsageMyTenantLogRequest logRequest)
        {
            if (!await ModelState.TryValidateObjectAsync(logRequest)) return BadRequest(ModelState);

            if (!logRequest.TrackName.IsNullOrWhiteSpace())
            {
                logRequest.TrackName = logRequest.TrackName.ToLower();
            }

            var logResponse = await usageLogLogic.GetTrackUsageLog(logRequest, RouteBinding.TenantName, logRequest.TrackName, isMasterTrack: true);
            return Ok(logResponse);
        }
    }
}
