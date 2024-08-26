using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Logic;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models.Config;
using System;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Usage)]
    public class TTrackLogUsageController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly UsageLogLogic usageLogLogic;

        public TTrackLogUsageController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, UsageLogLogic usageLogLogic) : base(logger)
        {
            this.settings = settings;
            this.usageLogLogic = usageLogLogic;
        }

        /// <summary>
        /// Get environment usage logs.
        /// </summary>
        /// <returns>Logs.</returns>
        [ProducesResponseType(typeof(Api.UsageLogResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.UsageLogResponse>> GetTrackLogUsage(Api.UsageLogRequest logRequest)
        {
            if (settings.Options.Log == LogOptions.Stdout)
            {
                throw new Exception("Not possible for Stdout.");
            }

            if (!await ModelState.TryValidateObjectAsync(logRequest)) return BadRequest(ModelState);

            var logResponse = await usageLogLogic.GetTrackUsageLog(logRequest, RouteBinding.TenantName, RouteBinding.TrackName);
            return Ok(logResponse);
        }
    }
}
