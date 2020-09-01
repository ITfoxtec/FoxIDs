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

namespace FoxIDs.Controllers
{
    public class TTrackKeyController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;

        public TTrackKeyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
        }

        /// <summary>
        /// Get track keys.
        /// </summary>
        /// <returns>Track keys.</returns>
        [ProducesResponseType(typeof(Api.TrackKeys), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackKeys>> GetTrackKey()
        {
            try
            {
                var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName});
                return Ok(mapper.Map<Api.TrackKeys>(mTrack));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.TrackKeys).Name}' by track name '{RouteBinding.TrackName}'.");
                    return NotFound(typeof(Api.TrackKeys).Name, RouteBinding.TrackName);
                }
                throw;
            }
        }

        /// <summary>
        /// Update track key.
        /// </summary>
        /// <param name="trackKeyRequest">Track key.</param>
        /// <returns>Track keys.</returns>
        [ProducesResponseType(typeof(Api.TrackKeys), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackKeys>> PutTrackKey([FromBody] Api.TrackKeyRequest trackKeyRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(trackKeyRequest)) return BadRequest(ModelState);
                try
                {
                    if (trackKeyRequest.Type != Api.TrackKeyType.Contained)
                    {
                        throw new ValidationException($"Currently only {Api.TrackKeyType.Contained} keys is supported in the API.");
                    }
                }
                catch (ValidationException vex)
                {
                    logger.Warning(vex);
                    ModelState.TryAddModelError(nameof(trackKeyRequest.Type), vex.Message);
                    return BadRequest(ModelState);
                }

                var mTrackKey = mapper.Map<TrackKey>(trackKeyRequest);
                try
                {
                    if (!mTrackKey.Key.HasPrivateKey)
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

                var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });
                if(trackKeyRequest.IsPrimary)
                {
                    mTrack.PrimaryKey = mTrackKey;
                }
                else
                {
                    mTrack.SecondaryKey = mTrackKey;
                }

                await tenantRepository.UpdateAsync(mTrack);

                return Ok(mapper.Map<Api.TrackKeys>(mTrack));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.TrackKeyRequest).Name}' by track name '{RouteBinding.TrackName}'.");
                    return NotFound(typeof(Api.TrackKeyRequest).Name, RouteBinding.TrackName);
                }
                throw;
            }
        }

        /// <summary>
        /// Delete secondary track key.
        /// </summary>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTrackKey()
        {
            try
            {
                var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });
                if(mTrack.SecondaryKey != null)
                {
                    mTrack.SecondaryKey = null;
                    await tenantRepository.UpdateAsync(mTrack);
                }

                return NoContent();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.TrackKey).Name}' by track name '{RouteBinding.TrackName}'.");
                    return NotFound(typeof(Api.TrackKey).Name, RouteBinding.TrackName);
                }
                throw;
            }
        }
    }
}
