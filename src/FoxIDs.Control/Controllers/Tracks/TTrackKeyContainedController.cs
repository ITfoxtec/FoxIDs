﻿using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using ITfoxtec.Identity;
using FoxIDs.Logic;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TTrackKeyContainedController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly TrackCacheLogic trackCacheLogic;

        public TTrackKeyContainedController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, TrackCacheLogic trackCacheLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.trackCacheLogic = trackCacheLogic;
        }

        /// <summary>
        /// Get environment keys contained.
        /// </summary>
        /// <returns>Track keys.</returns>
        [ProducesResponseType(typeof(Api.TrackKeyItemsContained), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackKeyItemsContained>> GetTrackKeyContained()
        {
            try
            {
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName});
                try
                {
                    if (mTrack.Key.Type != TrackKeyTypes.Contained)
                    {
                        throw new ValidationException($"Only {Api.TrackKeyTypes.Contained} keys is supported.");
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
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.TrackKeyItemsContained).Name}' contained by environment name '{RouteBinding.TrackName}'.");
                    return NotFound(typeof(Api.TrackKeyItemsContained).Name, RouteBinding.TrackName);
                }
                throw;
            }
        }

        /// <summary>
        /// Update environment key contained.
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
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(trackIdKey);
                try
                {
                    if (mTrack.Key.Type != TrackKeyTypes.Contained)
                    {
                        throw new ValidationException($"Only {Api.TrackKeyTypes.Contained} keys is supported.");
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
                    var certificateItem = await RouteBinding.CreateSelfSignedCertificateBySubjectAsync();
                    mTrackKey.Key = await certificateItem.Certificate.ToFTJsonWebKeyAsync(true);
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

                await tenantDataRepository.UpdateAsync(mTrack);

                await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);

                return Ok(mapper.Map<Api.TrackKeyItemsContained>(mTrack.Key));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.TrackKeyItemContainedRequest).Name}' contained by environment name '{RouteBinding.TrackName}'.");
                    return NotFound(typeof(Api.TrackKeyItemContainedRequest).Name, RouteBinding.TrackName);
                }
                throw;
            }
        }

        /// <summary>
        /// Delete secondary environment key contained.
        /// </summary>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTrackKeyContained()
        {
            try
            {
                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(trackIdKey);
                try
                {
                    if (mTrack.Key.Type != TrackKeyTypes.Contained)
                    {
                        throw new ValidationException($"Only {Api.TrackKeyTypes.Contained} keys is supported.");
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
                    await tenantDataRepository.UpdateAsync(mTrack);

                    await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);
                }

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.TrackKeyItemContained).Name}' contained by environment name '{RouteBinding.TrackName}'.");
                    return NotFound(typeof(Api.TrackKeyItemContained).Name, RouteBinding.TrackName);
                }
                throw;
            }
        }
    }
}
