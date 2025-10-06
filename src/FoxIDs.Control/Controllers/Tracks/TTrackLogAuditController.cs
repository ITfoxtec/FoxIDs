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
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Audit)]
    public class TTrackLogAuditController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly AuditLogLogic auditLogLogic;

        public TTrackLogAuditController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, AuditLogLogic auditLogLogic) : base(logger)
        {
            this.settings = settings;
            this.auditLogLogic = auditLogLogic;
        }

        /// <summary>
        /// Search audit logs for the current environment.
        /// </summary>
        /// <returns>Audit log entries.</returns>
        [ProducesResponseType(typeof(Api.LogResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LogResponse>> GetTrackLogAudit(Api.AuditLogRequest logRequest)
        {
            if (settings.Options.Log == LogOptions.Stdout)
            {
                throw new Exception("Audit log search is not supported with Stdout logging.");
            }

            if (!await ModelState.TryValidateObjectAsync(logRequest)) return BadRequest(ModelState);

            var logResponse = await auditLogLogic.GetAuditLogsAsync(logRequest, RouteBinding.TenantName, RouteBinding.TrackName);
            return Ok(logResponse);
        }
    }
}
