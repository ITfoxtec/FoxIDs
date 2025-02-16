using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Logic;
using ITfoxtec.Identity;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models.Config;
using System;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize(Constants.ControlApi.Segment.Log)]
    public class TTenantLogController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly LogLogic logLogic;

        public TTenantLogController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, LogLogic logLogic) : base(logger)
        {
            this.settings = settings;
            this.logLogic = logLogic;
        }

        /// <summary>
        /// Get tenant logs.
        /// </summary>
        /// <returns>Logs.</returns>
        [ProducesResponseType(typeof(Api.LogResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LogResponse>> GetTenantLog(Api.TenantLogRequest logRequest)
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

            if (!logRequest.TenantName.IsNullOrWhiteSpace())
            {
                logRequest.TenantName = logRequest.TenantName.ToLower();
                if (!logRequest.TrackName.IsNullOrWhiteSpace())
                {
                    logRequest.TrackName = logRequest.TrackName.ToLower();
                }
                else
                {
                    logRequest.TrackName = null;
                }
            }
            else
            {
                logRequest.TenantName = null;
                logRequest.TrackName = null;
            }

            var logResponse = await logLogic.QueryLogs(logRequest, logRequest.TenantName, logRequest.TrackName);
            return Ok(logResponse);
        }
    }
}
