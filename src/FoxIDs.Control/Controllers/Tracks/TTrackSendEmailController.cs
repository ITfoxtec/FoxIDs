﻿using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net;
using System;
using FoxIDs.Logic;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TTrackSendEmailController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;
        private readonly TrackCacheLogic trackCacheLogic;

        public TTrackSendEmailController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, TrackCacheLogic trackCacheLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
            this.trackCacheLogic = trackCacheLogic;
        }

        /// <summary>
        /// Get track send email.
        /// </summary>
        /// <returns>Send email.</returns>
        [ProducesResponseType(typeof(Api.ResourceItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SendEmail>> GetTrackSendEmail()
        {
            try
            {
                var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });
                if(mTrack.SendEmail == null)
                {
                    return NoContent();
                }
                return Ok(mapper.Map<Api.SendEmail>(mTrack.SendEmail));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get Track.SendEmail by track name '{RouteBinding.TrackName}'.");
                    return NotFound("Track.SendEmail", RouteBinding.TrackName);
                }
                throw;
            }
        }

        /// <summary>
        /// Update track send email.
        /// </summary>
        /// <param name="sendEmail">Send email.</param>
        /// <returns>Send email.</returns>
        [ProducesResponseType(typeof(Api.TrackResourceItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackResourceItem>> PutTrackSendEmail([FromBody] Api.SendEmail sendEmail)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(sendEmail)) return BadRequest(ModelState);

                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mTrack = await tenantRepository.GetTrackByNameAsync(trackIdKey);

                mTrack.SendEmail = mapper.Map<SendEmail>(sendEmail);
                await tenantRepository.UpdateAsync(mTrack);

                await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);

                return Ok(mapper.Map<Api.SendEmail>(mTrack.SendEmail));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update Track.SendEmail by track name '{RouteBinding.TrackName}'.");
                    return NotFound("Track.SendEmail", Convert.ToString(RouteBinding.TrackName));
                }
                throw;
            }
        }

        /// <summary>
        /// Delete track send email.
        /// </summary>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTrackSendEmail()
        {
            try
            {
                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mTrack = await tenantRepository.GetTrackByNameAsync(trackIdKey);

                mTrack.SendEmail = null;
                await tenantRepository.UpdateAsync(mTrack);

                await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);

                return NoContent();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete Track.SendEmail by track name '{RouteBinding.TrackName}'.");
                    return NotFound("Track.SendEmail", Convert.ToString(RouteBinding.TrackName));
                }
                throw;
            }
        }
    }
}
