using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Logic;
using ITfoxtec.Identity;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models.Config;
using System;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Audit)]
    public class TMyTenantLogAuditController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly AuditLogLogic auditLogLogic;

        public TMyTenantLogAuditController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, AuditLogLogic auditLogLogic) : base(logger)
        {
            this.settings = settings;
            this.auditLogLogic = auditLogLogic;
        }

        /// <summary>
        /// Get my tenant audit logs.
        /// </summary>
        /// <returns>Audit logs.</returns>
        [ProducesResponseType(typeof(Api.LogResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LogResponse>> GetMyTenantLogAudit(Api.TenantAuditLogRequest logRequest)
        {
            if (settings.Options.Log == LogOptions.Stdout)
            {
                throw new Exception("Audit log search is not supported with Stdout logging.");
            }

            if (!await ModelState.TryValidateObjectAsync(logRequest)) return BadRequest(ModelState);

            if (!logRequest.TrackName.IsNullOrWhiteSpace())
            {
                logRequest.TrackName = logRequest.TrackName.ToLower();
            }
            else
            {
                logRequest.TrackName = null;
            }

            var logResponse = await auditLogLogic.GetAuditLogsAsync(logRequest, RouteBinding.TenantName, logRequest.TrackName);
            return Ok(logResponse);
        }
    }
}
