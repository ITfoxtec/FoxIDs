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

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TTrackResourceSettingController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;
        private readonly TrackCacheLogic trackCacheLogic;

        public TTrackResourceSettingController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, TrackCacheLogic trackCacheLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
            this.trackCacheLogic = trackCacheLogic;
        }

        /// <summary>
        /// Get track resource settings.
        /// </summary>
        /// <returns>Resource settings.</returns>
        [ProducesResponseType(typeof(Api.ResourceSettings), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.ResourceSettings>> GetTrackResourceSetting()
        {
            try
            {
                var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });
                return new Api.ResourceSettings { ShowResourceId = mTrack.ShowResourceId };
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get {nameof(Track)} by track name '{RouteBinding.TrackName}'.");
                    return NotFound(nameof(Track), RouteBinding.TrackName);
                }
                throw;
            }
        }

        /// <summary>
        /// Save track resource settings.
        /// </summary>
        /// <param name="resourceSettings">Resource settings.</param>
        /// <returns>Resource settings.</returns>
        [ProducesResponseType(typeof(Api.ResourceSettings), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.ResourceSettings>> PostTrackResourceSetting([FromBody] Api.ResourceSettings resourceSettings)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(resourceSettings)) return BadRequest(ModelState);

                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mTrack = await tenantRepository.GetTrackByNameAsync(trackIdKey);
                if(mTrack.ShowResourceId != resourceSettings.ShowResourceId)
                {
                    mTrack.ShowResourceId = resourceSettings.ShowResourceId;
                    await tenantRepository.UpdateAsync(mTrack);

                    await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);
                }

                return Ok(resourceSettings);
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Save {nameof(Track)} by track name '{RouteBinding.TrackName}'.");
                    return NotFound(nameof(Track), RouteBinding.TrackName);
                }
                throw;
            }
        }
    }
}
