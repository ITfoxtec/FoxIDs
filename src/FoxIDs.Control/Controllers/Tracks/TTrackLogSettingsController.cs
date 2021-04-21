using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net;

namespace FoxIDs.Controllers
{
    public class TTrackLogSettingsController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;

        public TTrackLogSettingsController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
        }

        /// <summary>
        /// Get track log settings.
        /// </summary>
        /// <returns>Log settings.</returns>
        [ProducesResponseType(typeof(Api.LogSettings), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LogSettings>> GetTrackLogSettings()
        {
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
                    logger.Warning(ex, $"NotFound, Get {nameof(Track)}.{nameof(Track.Logging)}.{nameof(Track.Logging.ScopedLogger)} by track name '{RouteBinding.TrackName}'.");
                    return NotFound($"{nameof(Track)}.{nameof(Track.Logging)}.{nameof(Track.Logging.ScopedLogger)}", RouteBinding.TrackName);
                }
                throw;
            }
        }

        /// <summary>
        /// Save track log settings.
        /// </summary>
        /// <param name="logSettings">Log settings.</param>
        /// <returns>Log settings.</returns>
        [ProducesResponseType(typeof(Api.LogSettings), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.LogSettings>> PostTrackLogSettings([FromBody] Api.LogSettings logSettings)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(logSettings)) return BadRequest(ModelState);

                var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });
                if (mTrack.Logging == null)
                {
                    mTrack.Logging = new Logging();
                }
                mTrack.Logging.ScopedLogger = mapper.Map<ScopedLogger>(logSettings);
                await tenantRepository.UpdateAsync(mTrack);

                return Ok(mapper.Map<Api.LogSettings>(mTrack.Logging.ScopedLogger));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Save {nameof(Track)}.{nameof(Track.Logging)}.{nameof(Track.Logging.ScopedLogger)} by track name '{RouteBinding.TrackName}'.");
                    return NotFound($"{nameof(Track)}.{nameof(Track.Logging)}.{nameof(Track.Logging.ScopedLogger)}", RouteBinding.TrackName);
                }
                throw;
            }
        }
    }
}
