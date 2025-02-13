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
    [MasterScopeAuthorize(Constants.ControlApi.Segment.Usage)]
    public class TTenantLogUsageController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly UsageLogLogic usageLogLogic;

        public TTenantLogUsageController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, UsageLogLogic usageLogLogic) : base(logger)
        {
            this.settings = settings;
            this.usageLogLogic = usageLogLogic;
        }

        /// <summary>
        /// Get tenant usage logs.
        /// </summary>
        /// <returns>Logs.</returns>
        [ProducesResponseType(typeof(Api.UsageLogResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.UsageLogResponse>> GetTenantLogUsage(Api.UsageTenantLogRequest logRequest)
        {
            if (settings.Options.Log == LogOptions.Stdout)
            {
                throw new Exception("Not possible for Stdout.");
            }

            if (!await ModelState.TryValidateObjectAsync(logRequest)) return BadRequest(ModelState);

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

            var logResponse = await usageLogLogic.GetTrackUsageLogAsync(logRequest, logRequest.TenantName, logRequest.TrackName, isMasterTenant: true);
            return Ok(logResponse);
        }
    }
}
