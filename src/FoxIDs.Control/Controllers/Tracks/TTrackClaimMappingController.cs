using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Controllers
{
    public class TTrackClaimMappingController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;

        public TTrackClaimMappingController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
        }

        /// <summary>
        /// Get track claim mappings.
        /// </summary>
        /// <returns>Claim mappings.</returns>
        [ProducesResponseType(typeof(List<Api.ClaimMap>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<Api.ClaimMap>>> GetTrackClaimMapping()
        {
            try
            {
                var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });
                if (mTrack.ClaimMappings?.Count > 0)
                {
                    return Ok(mapper.Map<List<Api.ClaimMap>>(mTrack.ClaimMappings));
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
                    logger.Warning(ex, $"NotFound, Get Track.ClaimMappings by track name '{RouteBinding.TrackName}'.");
                    return NotFound("Track.ClaimMappings", RouteBinding.TrackName);
                }
                throw;
            }
        }

        /// <summary>
        /// Save track claim mappings.
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

                var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });

                mTrack.ClaimMappings = mapper.Map<List<ClaimMap>>(claimMappings);
                await tenantRepository.UpdateAsync(mTrack);

                return Ok(mapper.Map<List<Api.ClaimMap>>(mTrack.SendEmail));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Save Track.ClaimMappings by track name '{RouteBinding.TrackName}'.");
                    return NotFound("Track.ClaimMappings", RouteBinding.TrackName);
                }
                throw;
            }
        }
    }
}
