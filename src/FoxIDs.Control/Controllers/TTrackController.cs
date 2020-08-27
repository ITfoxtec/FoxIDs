﻿using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net;
using ITfoxtec.Identity;

namespace FoxIDs.Controllers
{
    public class TTrackController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;

        public TTrackController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
        }

        /// <summary>
        /// Get track.
        /// </summary>
        /// <param name="name">Track name.</param>
        /// <returns>Track.</returns>
        [ProducesResponseType(typeof(Api.Track), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.Track>> GetTrack(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);

                var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = name});
                return Ok(mapper.Map<Api.Track>(mTrack));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.Track).Name}' by name '{name}'.");
                    return NotFound(typeof(Api.Track).Name, name);
                }
                throw;
            }
        }

        /// <summary>
        /// Create track.
        /// </summary>
        /// <param name="track">Track.</param>
        /// <returns>Track.</returns>
        [ProducesResponseType(typeof(Api.Track), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult<Api.Track>> PostTrack([FromBody] Api.Track track)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(track)) return BadRequest(ModelState);

                var mTrack = mapper.Map<Track>(track);

                var certificate = await track.Name.CreateSelfSignedCertificateAsync();
                mTrack.PrimaryKey = new TrackKey()
                {
                    Type = TrackKeyType.Contained,
                    Key = await certificate.ToJsonWebKeyAsync(true)
                };
                await tenantRepository.CreateAsync(mTrack);

                return Created(mapper.Map<Api.Track>(mTrack));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create '{typeof(Api.Track).Name}' by name '{track.Name}'.");
                    return Conflict(typeof(Api.Track).Name, track.Name);
                }
                throw;
            }
        }

        ///// <summary>
        ///// Update track.
        ///// </summary>
        ///// <param name="tenant">Track.</param>
        ///// <returns>Track.</returns>
        //[ProducesResponseType(typeof(Api.Track), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //public async Task<ActionResult<Api.Track>> PutTrack([FromBody] Api.Track track)
        //{
        //    try
        //    {
        //        if (!await ModelState.TryValidateObjectAsync(track)) return BadRequest(ModelState);

        //        var mTrack = mapper.Map<Track>(track);
        //        await tenantRepository.UpdateAsync(mTrack);

        //        return Created(mapper.Map<Api.Track>(mTrack));
        //    }
        //    catch (CosmosDataException ex)
        //    {
        //        if (ex.StatusCode == HttpStatusCode.NotFound)
        //        {
        //            logger.Warning(ex, $"NotFound, Update '{typeof(Api.Track).Name}' by name '{tenant.Name}'.");
        //            return NotFound(typeof(Api.Track).Name, tenant.Name);
        //        }
        //        throw;
        //    }
        //}

        /// <summary>
        /// Delete track.
        /// </summary>
        /// <param name="name">Track name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTrack(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);

                await tenantRepository.DeleteAsync<Track>(await Track.IdFormat(RouteBinding, name));
                return NoContent();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.Track).Name}' by name '{name}'.");
                    return NotFound(typeof(Api.Track).Name, name);
                }
                throw;
            }
        }
    }
}
