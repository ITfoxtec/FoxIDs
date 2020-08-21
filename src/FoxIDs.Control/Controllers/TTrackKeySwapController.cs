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
    public class TTrackKeySwapController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantService;

        public TTrackKeySwapController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantService) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantService = tenantService;
        }

        /// <summary>
        /// Swap track key.
        /// </summary>
        /// <param name="trackKeySwap">Track to swap.</param>
        /// <returns>Track keys.</returns>
        [ProducesResponseType(typeof(Api.Track), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult<Api.TrackKeys>> PostTrackKeySwap([FromBody] Api.TrackKeySwap trackKeySwap)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(trackKeySwap)) return BadRequest(ModelState);

                var mTrack = await tenantService.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = trackKeySwap.TrackName });
                try
                {
                    if (mTrack.SecondaryKey == null)
                    {
                        throw new ValidationException("Secondary key is required.");
                    }
                }
                catch (ValidationException vex)
                {
                    logger.Warning(vex);
                    ModelState.TryAddModelError(nameof(trackKeySwap.TrackName), vex.Message);
                    return BadRequest(ModelState);
                }

                var tempSecondaryKey = mTrack.SecondaryKey;
                mTrack.SecondaryKey = mTrack.PrimaryKey;
                mTrack.PrimaryKey = tempSecondaryKey;

                await tenantService.UpdateAsync(mTrack);

                return Created(mapper.Map<Api.TrackKeys>(mTrack));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Swap Track key '{typeof(Api.TrackKeySwap).Name}' by track name '{trackKeySwap.TrackName}'.");
                    return NotFound(typeof(Api.TrackKeySwap).Name, trackKeySwap.TrackName);
                }
                throw;
            }
        }
    }
}
