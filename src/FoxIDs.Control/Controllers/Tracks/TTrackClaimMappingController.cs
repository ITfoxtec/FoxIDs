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
using System.Linq;
using FoxIDs.Logic;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TTrackClaimMappingController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly TrackCacheLogic trackCacheLogic;

        public TTrackClaimMappingController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, TrackCacheLogic trackCacheLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.trackCacheLogic = trackCacheLogic;
        }

        /// <summary>
        /// Get environment claim mappings.
        /// </summary>
        /// <returns>Claim mappings.</returns>
        [ProducesResponseType(typeof(List<Api.ClaimMap>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<Api.ClaimMap>>> GetTrackClaimMapping()
        {
            try
            {
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });
                if (mTrack.ClaimMappings?.Count > 0)
                {
                    return Ok(mapper.Map<List<Api.ClaimMap>>(mTrack.ClaimMappings));
                }
                else
                {
                    return NoContent();
                }
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get {nameof(Track)}.{nameof(Track.ClaimMappings)} by track name '{RouteBinding.TrackName}'.");
                    return NotFound($"{nameof(Track)}.{nameof(Track.ClaimMappings)}", RouteBinding.TrackName);
                }
                throw;
            }
        }

        /// <summary>
        /// Save environment claim mappings.
        /// </summary>
        /// <param name="claimMappings">Claim mappings.</param>
        /// <returns>Claim mappings.</returns>
        [ProducesResponseType(typeof(List<Api.ClaimMap>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<Api.ClaimMap>>> PostTrackClaimMapping([FromBody] List<Api.ClaimMap> claimMappings)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(claimMappings)) return BadRequest(ModelState);

                var duplicatedJwtClaimMappings = claimMappings.GroupBy(cm => cm.JwtClaim).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
                if (duplicatedJwtClaimMappings != null)
                {
                    ModelState.TryAddModelError(string.Empty, $"Duplicated JWT claim mappings '{duplicatedJwtClaimMappings}'");
                    return BadRequest(ModelState);
                }

                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(trackIdKey);

                mTrack.ClaimMappings = mapper.Map<List<ClaimMap>>(claimMappings);
                await tenantDataRepository.UpdateAsync(mTrack);

                await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);

                return Ok(mapper.Map<List<Api.ClaimMap>>(mTrack.ClaimMappings));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Save {nameof(Track)}.{nameof(Track.ClaimMappings)} by environment name '{RouteBinding.TrackName}'.");
                    return NotFound($"{nameof(Track)}.{nameof(Track.ClaimMappings)}", RouteBinding.TrackName);
                }
                throw;
            }
        }
    }
}
