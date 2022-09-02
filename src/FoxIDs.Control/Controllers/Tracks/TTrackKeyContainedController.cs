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
using ITfoxtec.Identity;
using FoxIDs.Logic;

namespace FoxIDs.Controllers
{
    public class TTrackKeyContainedController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;
        private readonly TrackCacheLogic trackCacheLogic;

        public TTrackKeyContainedController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, TrackCacheLogic trackCacheLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
            this.trackCacheLogic = trackCacheLogic;
        }

        /// <summary>
        /// Get track keys contained.
        /// </summary>
        /// <returns>Track keys.</returns>
        [ProducesResponseType(typeof(Api.TrackKeyItemsContained), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackKeyItemsContained>> GetTrackKeyContained()
        {
            try
            {
                var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName});
                try
                {
                    if (mTrack.Key.Type != TrackKeyType.Contained)
                    {
                        throw new ValidationException($"Only {Api.TrackKeyType.Contained} keys is supported.");
                    }
                }
                catch (ValidationException vex)
                {
                    logger.Warning(vex);
                    ModelState.TryAddModelError(string.Empty, vex.Message);
                    return BadRequest(ModelState);
                }

                return Ok(mapper.Map<Api.TrackKeyItemsContained>(mTrack.Key));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.TrackKeyItemsContained).Name}' contained by track name '{RouteBinding.TrackName}'.");
                    return NotFound(typeof(Api.TrackKeyItemsContained).Name, RouteBinding.TrackName);
                }
                throw;
            }
        }

        /// <summary>
        /// Update track key contained.
        /// </summary>
        /// <param name="trackKeyRequest">Track key.</param>
        /// <returns>Track keys.</returns>
        [ProducesResponseType(typeof(Api.TrackKeyItemsContained), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackKeyItemsContained>> PutTrackKeyContained([FromBody] Api.TrackKeyItemContainedRequest trackKeyRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(trackKeyRequest)) return BadRequest(ModelState);

                var mTrackKey = mapper.Map<TrackKeyItem>(trackKeyRequest);
                if (!trackKeyRequest.CreateSelfSigned)
                {
                    try
                    {
                        if (!mTrackKey.Key.HasPrivateKey())
                        {
                            throw new ValidationException("Private key is required.");
                        }
                    }
                    catch (ValidationException vex)
                    {
                        logger.Warning(vex);
                        ModelState.TryAddModelError(nameof(trackKeyRequest.Key), vex.Message);
                        return BadRequest(ModelState);
                    }
                }

                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mTrack = await tenantRepository.GetTrackByNameAsync(trackIdKey);
                try
                {
                    if (mTrack.Key.Type != TrackKeyType.Contained)
                    {
                        throw new ValidationException($"Only {Api.TrackKeyType.Contained} keys is supported.");
                    }
                }
                catch (ValidationException vex)
                {
                    logger.Warning(vex);
                    ModelState.TryAddModelError(string.Empty, vex.Message);
                    return BadRequest(ModelState);
                }

                if (trackKeyRequest.CreateSelfSigned)
                {
                    var certificate = await RouteBinding.CreateSelfSignedCertificateBySubjectAsync();
                    mTrackKey.Key = await certificate.ToFTJsonWebKeyAsync(true);
                }

                if (trackKeyRequest.IsPrimary)
                {
                    mTrack.Key.Keys[0] = mTrackKey;
                }
                else
                {
                    if(mTrack.Key.Keys.Count > 1)
                    {
                        mTrack.Key.Keys[1] = mTrackKey;
                    }
                    else
                    {
                        mTrack.Key.Keys.Add(mTrackKey);
                    }
                }

                await tenantRepository.UpdateAsync(mTrack);

                await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);

                return Ok(mapper.Map<Api.TrackKeyItemsContained>(mTrack.Key));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.TrackKeyItemContainedRequest).Name}' contained by track name '{RouteBinding.TrackName}'.");
                    return NotFound(typeof(Api.TrackKeyItemContainedRequest).Name, RouteBinding.TrackName);
                }
                throw;
            }
        }

        /// <summary>
        /// Delete secondary track key contained.
        /// </summary>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTrackKeyContained()
        {
            try
            {
                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mTrack = await tenantRepository.GetTrackByNameAsync(trackIdKey);
                try
                {
                    if (mTrack.Key.Type != TrackKeyType.Contained)
                    {
                        throw new ValidationException($"Only {Api.TrackKeyType.Contained} keys is supported.");
                    }
                }
                catch (ValidationException vex)
                {
                    logger.Warning(vex);
                    ModelState.TryAddModelError(string.Empty, vex.Message);
                    return BadRequest(ModelState);
                }

                if (mTrack.Key.Keys.Count > 1)
                {
                    mTrack.Key.Keys.RemoveAt(1);
                    await tenantRepository.UpdateAsync(mTrack);

                    await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);
                }

                return NoContent();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.TrackKeyItemContained).Name}' contained by track name '{RouteBinding.TrackName}'.");
                    return NotFound(typeof(Api.TrackKeyItemContained).Name, RouteBinding.TrackName);
                }
                throw;
            }
        }
    }
}
