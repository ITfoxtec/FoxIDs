using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Logic;
using ITfoxtec.Identity;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models.Config;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Log)]
    public class TMyTenantLogController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly LogLogic logLogic;

        public TMyTenantLogController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, LogLogic logLogic) : base(logger)
        {
            this.settings = settings;
            this.logLogic = logLogic;
        }

        /// <summary>
        /// Get my tenant logs.
        /// </summary>
        /// <returns>Logs.</returns>
        [ProducesResponseType(typeof(Api.LogResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LogResponse>> GetMyTenantLog(Api.MyTenantLogRequest logRequest)
        {
            if (!await ModelState.TryValidateObjectAsync(logRequest)) return BadRequest(ModelState);

            if (settings.Options.Log == LogOptions.ApplicationInsights)
            {
                if (logRequest.QueryTraces && logRequest.QueryEvents)
                {
                    ModelState.AddModelError(nameof(logRequest.QueryTraces), $"The field {nameof(logRequest)}.{nameof(logRequest.QueryTraces)} and {nameof(logRequest)}.{nameof(logRequest.QueryEvents)} cannot be true at the same time.");
                    return BadRequest(ModelState);
                }
            }

            if (!logRequest.TrackName.IsNullOrWhiteSpace())
            {
                logRequest.TrackName = logRequest.TrackName.ToLower();
            }
            else
            {
                logRequest.TrackName = null;
            }

            var logResponse = await logLogic.QueryLogs(logRequest, RouteBinding.TenantName, logRequest.TrackName);
            return Ok(logResponse);
        }
    }
}
