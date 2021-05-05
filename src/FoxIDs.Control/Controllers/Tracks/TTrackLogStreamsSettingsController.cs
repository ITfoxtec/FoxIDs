using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Generic;

namespace FoxIDs.Controllers
{
    public class TTrackLogStreamsSettingsController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;

        public TTrackLogStreamsSettingsController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
        }

        /// <summary>
        /// Get all track log stream settings.
        /// </summary>
        /// <returns>All log stream settings.</returns>
        [ProducesResponseType(typeof(Api.LogStreams), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LogStreams>> GetTrackLogStreamsSettings()
        {
            try
            {
                var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });
                if (mTrack.Logging != null && mTrack.Logging.ScopedStreamLoggers?.Count > 0)
                {
                    return Ok(new Api.LogStreams { LogStreamSettings = mapper.Map<List<Api.LogStreamSettings>>(mTrack.Logging.ScopedStreamLoggers) });
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
                    logger.Warning(ex, $"NotFound, Get {nameof(Track)}.{nameof(Track.Logging)}.{nameof(Track.Logging.ScopedStreamLoggers)} by track name '{RouteBinding.TrackName}'.");
                    return NotFound($"{nameof(Track)}.{nameof(Track.Logging)}.{nameof(Track.Logging.ScopedStreamLoggers)}", RouteBinding.TrackName);
                }
                throw;
            }
        }

        /// <summary>
        /// Save all track log stream settings.
        /// </summary>
        /// <param name="logStreams">All log stream settings.</param>
        /// <returns>All log stream settings.</returns>
        [ProducesResponseType(typeof(Api.LogStreams), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LogStreams>> PostTrackLogStreamsSettings([FromBody] Api.LogStreams logStreams)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(logStreams)) return BadRequest(ModelState);

                var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });
                if (mTrack.Logging == null)
                {
                    mTrack.Logging = new Logging();
                }
                mTrack.Logging.ScopedStreamLoggers = mapper.Map<List<ScopedStreamLogger>>(logStreams.LogStreamSettings);
                await tenantRepository.UpdateAsync(mTrack);

                return Ok(mapper.Map<HashSet<Api.LogStreamSettings>>(mTrack.Logging.ScopedStreamLoggers));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Save {nameof(Track)}.{nameof(Track.Logging)}.{nameof(Track.Logging.ScopedStreamLoggers)} by track name '{RouteBinding.TrackName}'.");
                    return NotFound($"{nameof(Track)}.{nameof(Track.Logging)}.{nameof(Track.Logging.ScopedStreamLoggers)}", RouteBinding.TrackName);
                }
                throw;
            }
        }
    }
}
