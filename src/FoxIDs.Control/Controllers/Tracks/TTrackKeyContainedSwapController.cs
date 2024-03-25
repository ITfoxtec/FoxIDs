using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net;
using System.ComponentModel.DataAnnotations;
using FoxIDs.Logic;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TTrackKeyContainedSwapController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantRepository;
        private readonly TrackCacheLogic trackCacheLogic;

        public TTrackKeyContainedSwapController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantRepository, TrackCacheLogic trackCacheLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
            this.trackCacheLogic = trackCacheLogic;
        }

        /// <summary>
        /// Swap environment key contained.
        /// </summary>
        /// <param name="trackKeySwap">Track to swap.</param>
        /// <returns>Track keys.</returns>
        [ProducesResponseType(typeof(Api.TrackKeyItemsContained), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackKeyItemsContained>> PostTrackKeyContainedSwap([FromBody] Api.TrackKeyItemContainedSwap trackKeySwap)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(trackKeySwap)) return BadRequest(ModelState);
                try
                {
                    if (!trackKeySwap.SwapKeys)
                    {
                        throw new ValidationException($"Required '{nameof(trackKeySwap.SwapKeys)}' to be true.");
                    }
                }
                catch (ValidationException vex)
                {
                    logger.Warning(vex);
                    ModelState.TryAddModelError(nameof(trackKeySwap.SwapKeys), vex.Message);
                    return BadRequest(ModelState);
                }

                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mTrack = await tenantRepository.GetTrackByNameAsync(trackIdKey);
                try
                {
                    if (mTrack.Key.Type != TrackKeyTypes.Contained)
                    {
                        throw new ValidationException($"Only {Api.TrackKeyTypes.Contained} keys is supported.");
                    }
                    if (mTrack.Key.Keys.Count < 2)
                    {
                        throw new ValidationException("Secondary key is required.");
                    }
                }
                catch (ValidationException vex)
                {
                    logger.Warning(vex);
                    ModelState.TryAddModelError(nameof(trackKeySwap.SwapKeys), vex.Message);
                    return BadRequest(ModelState);
                }

                var tempSecondaryKey = mTrack.Key.Keys[1];
                mTrack.Key.Keys[1] = mTrack.Key.Keys[0];
                mTrack.Key.Keys[0] = tempSecondaryKey;

                await tenantRepository.UpdateAsync(mTrack);

                await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);

                return Created(mapper.Map<Api.TrackKeyItemsContained>(mTrack.Key));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Swap Environment key contained '{typeof(Api.TrackKeyItemContainedSwap).Name}' by environment name '{RouteBinding.TrackName}'.");
                    return NotFound(typeof(Api.TrackKeyItemContainedSwap).Name, RouteBinding.TrackName);
                }
                throw;
            }
        }
    }
}
