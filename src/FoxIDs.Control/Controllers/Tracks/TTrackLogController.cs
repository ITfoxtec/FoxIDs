using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Models.Config;
using System;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Logic;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Log)]
    public class TTrackLogController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly LogLogic logLogic;

        public TTrackLogController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, LogLogic logLogic) : base(logger)
        {
            this.settings = settings;
            this.logLogic = logLogic;
        }

        /// <summary>
        /// Get environment logs.
        /// </summary>
        /// <returns>Logs.</returns>
        [ProducesResponseType(typeof(Api.LogResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LogResponse>> GetTrackLog(Api.LogRequest logRequest)
        {
            if (settings.Options.Log == LogOptions.Stdout)
            {
                throw new Exception("Not possible for Stdout.");
            }

            if (!await ModelState.TryValidateObjectAsync(logRequest)) return BadRequest(ModelState);

            if (settings.Options.Log == LogOptions.ApplicationInsights)
            {
                if (logRequest.QueryTraces && logRequest.QueryEvents)
                {
                    ModelState.AddModelError(nameof(logRequest.QueryTraces), $"The field {nameof(logRequest)}.{nameof(logRequest.QueryTraces)} and {nameof(logRequest)}.{nameof(logRequest.QueryEvents)} cannot be true at the same time.");
                    return BadRequest(ModelState);
                }
            }

            var logResponse = await logLogic.QueryLogs(logRequest, RouteBinding.TenantName, RouteBinding.TrackName);
            return Ok(logResponse);
        }
    }
}
