using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net;
using FoxIDs.Logic;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models.Config;
using System;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Log)]
    public class TTrackLogSettingController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;
        private readonly TrackCacheLogic trackCacheLogic;

        public TTrackLogSettingController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, TrackCacheLogic trackCacheLogic) : base(logger)
        {
            this.settings = settings;
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
            this.trackCacheLogic = trackCacheLogic;
        }

        /// <summary>
        /// Get environment log settings.
        /// </summary>
        /// <returns>Log settings.</returns>
        [ProducesResponseType(typeof(Api.LogSettings), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LogSettings>> GetTrackLogSetting()
        {
            if (settings.Options.Log != LogOptions.ApplicationInsights)
            {
                throw new Exception("ApplicationInsights option not enabled.");
            }

            try
            {
                var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });
                if (mTrack.Logging != null && mTrack.Logging.ScopedLogger != null)
                {
                    return Ok(mapper.Map<Api.LogSettings>(mTrack.Logging.ScopedLogger));
                }
                else
                {
                    return NoContent();
                }
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get {nameof(Track)}.{nameof(Track.Logging)}.{nameof(Track.Logging.ScopedLogger)} by environment name '{RouteBinding.TrackName}'.");
                    return NotFound($"{nameof(Track)}.{nameof(Track.Logging)}.{nameof(Track.Logging.ScopedLogger)}", RouteBinding.TrackName);
                }
                throw;
            }
        }

        /// <summary>
        /// Save environment log settings.
        /// </summary>
        /// <param name="logSettings">Log settings.</param>
        /// <returns>Log settings.</returns>
        [ProducesResponseType(typeof(Api.LogSettings), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LogSettings>> PostTrackLogSetting([FromBody] Api.LogSettings logSettings)
        {
            if (settings.Options.Log != LogOptions.ApplicationInsights)
            {
                throw new Exception("ApplicationInsights option not enabled.");
            }

            try
            {
                if (!await ModelState.TryValidateObjectAsync(logSettings)) return BadRequest(ModelState);

                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mTrack = await tenantRepository.GetTrackByNameAsync(trackIdKey);
                if (mTrack.Logging == null)
                {
                    mTrack.Logging = new Logging();
                }
                mTrack.Logging.ScopedLogger = mapper.Map<ScopedLogger>(logSettings);
                await tenantRepository.UpdateAsync(mTrack);

                await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);

                return Ok(mapper.Map<Api.LogSettings>(mTrack.Logging.ScopedLogger));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Save {nameof(Track)}.{nameof(Track.Logging)}.{nameof(Track.Logging.ScopedLogger)} by environment name '{RouteBinding.TrackName}'.");
                    return NotFound($"{nameof(Track)}.{nameof(Track.Logging)}.{nameof(Track.Logging.ScopedLogger)}", RouteBinding.TrackName);
                }
                throw;
            }
        }
    }
}
