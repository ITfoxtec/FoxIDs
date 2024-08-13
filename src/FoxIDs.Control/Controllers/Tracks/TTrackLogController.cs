using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Models.Config;
using System;
using ITfoxtec.Identity;
using Azure.Monitor.Query;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Logic;
using Microsoft.Extensions.DependencyInjection;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Log)]
    public class TTrackLogController : ApiController
    {
        private const int maxQueryLogItems = 200;
        private const int maxResponseLogItems = 300;
        private readonly FoxIDsControlSettings settings;
        private readonly IServiceProvider serviceProvider;

        public TTrackLogController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IServiceProvider serviceProvider) : base(logger)
        {
            this.settings = settings;
            this.serviceProvider = serviceProvider;
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

            if (!logRequest.Filter.IsNullOrEmpty())
            {
                logRequest.Filter = logRequest.Filter.Trim();
            }

            if (!logRequest.QueryExceptions && !logRequest.QueryTraces && !logRequest.QueryEvents && !logRequest.QueryMetrics)
            {
                logRequest.QueryExceptions = true;
                logRequest.QueryEvents = true;
            }

            var start = DateTimeOffset.FromUnixTimeSeconds(logRequest.FromTime);
            var end = DateTimeOffset.FromUnixTimeSeconds(logRequest.ToTime);

            switch (settings.Options.Log)
            {
                case LogOptions.OpenSearchAndStdoutErrors:
                    return Ok(await serviceProvider.GetService<LogOpenSearchLogic>().QueryLogs(logRequest, (start, end), maxQueryLogItems, maxResponseLogItems));
                case LogOptions.ApplicationInsights:
                    return Ok(await serviceProvider.GetService<LogApplicationInsightsLogic>().QueryLogs(logRequest, new QueryTimeRange(start, end), maxQueryLogItems, maxResponseLogItems));
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
