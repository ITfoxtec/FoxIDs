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
using ITfoxtec.Identity;
using System;
using FoxIDs.Infrastructure.Security;
using ITfoxtec.Identity.Util;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TTrackController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;
        private readonly PlanCacheLogic planCacheLogic;
        private readonly TrackCacheLogic trackCacheLogic;
        private readonly TrackLogic trackLogic;
        private readonly ExternalKeyLogic externalKeyLogic;

        public TTrackController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, PlanCacheLogic planCacheLogic, TrackCacheLogic trackCacheLogic, TrackLogic trackLogic, ExternalKeyLogic externalKeyLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
            this.planCacheLogic = planCacheLogic;
            this.trackCacheLogic = trackCacheLogic;
            this.trackLogic = trackLogic;
            this.externalKeyLogic = externalKeyLogic;
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
                name = name?.ToLower();

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
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.Track>> PostTrack([FromBody] Api.Track track)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(track)) return BadRequest(ModelState);
                track.Name = await GetTrackNameAsync(track.Name);

                if (!RouteBinding.PlanName.IsNullOrEmpty())
                {
                    var plan = await planCacheLogic.GetPlanAsync(RouteBinding.PlanName);
                    if (plan.Tracks.IsLimited)
                    {
                        var count = await tenantRepository.CountAsync<Track>(new Track.IdKey { TenantName = RouteBinding.TenantName });
                        // included + master track
                        if (count > plan.Tracks.Included) 
                        {
                            throw new Exception($"Maximum number of tracks ({plan.Tracks.Included}) included in the '{plan.Name}' plan has been reached. Master environment not counted.");
                        }
                    }
                }

                var mTrack = mapper.Map<Track>(track);
                await trackLogic.CreateTrackDocumentAsync(mTrack, await GetKeyTypeAsync());
                await trackLogic.CreateLoginDocumentAsync(mTrack);

                return Created(mapper.Map<Api.Track>(mTrack));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create '{typeof(Api.Track).Name}' by name '{track.Name}'.");
                    return Conflict(typeof(Api.Track).Name, track.Name, nameof(track.Name));
                }
                throw;
            }
        }

        private async Task<TrackKeyTypes> GetKeyTypeAsync()
        {
            Plan plan = null;
            if (!RouteBinding.PlanName.IsNullOrEmpty())
            {            
                plan = await planCacheLogic.GetPlanAsync(RouteBinding.PlanName);                
            }
            return plan.GetKeyType();
        }

        /// <summary>
        /// Update track.
        /// </summary>
        /// <param name="track">Track.</param>
        /// <returns>Track.</returns>
        [ProducesResponseType(typeof(Api.Track), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.Track>> PutTrack([FromBody] Api.Track track)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(track)) return BadRequest(ModelState);
                track.Name = await GetTrackNameAsync(track.Name);

                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = track.Name };
                var mTrack = await tenantRepository.GetTrackByNameAsync(trackIdKey);
                mTrack.DisplayName = track.DisplayName;
                mTrack.SequenceLifetime = track.SequenceLifetime;
                mTrack.MaxFailingLogins = track.MaxFailingLogins;
                mTrack.FailingLoginCountLifetime = track.FailingLoginCountLifetime;
                mTrack.FailingLoginObservationPeriod = track.FailingLoginObservationPeriod;
                mTrack.PasswordLength = track.PasswordLength;
                mTrack.CheckPasswordComplexity = track.CheckPasswordComplexity;
                mTrack.CheckPasswordRisk = track.CheckPasswordRisk;
                mTrack.AllowIframeOnDomains = track.AllowIframeOnDomains;
                await tenantRepository.UpdateAsync(mTrack);

                await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);

                return Ok(mapper.Map<Api.Track>(mTrack));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.Track).Name}' by name '{track.Name}'.");
                    return NotFound(typeof(Api.Track).Name, track.Name, nameof(track.Name));
                }
                throw;
            }
        }

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
                name = name?.ToLower();

                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = name };
                var mTrack = await tenantRepository.GetTrackByNameAsync(trackIdKey);

                await tenantRepository.DeleteListAsync<DefaultElement>(trackIdKey);
                await tenantRepository.DeleteAsync<Track>(await Track.IdFormatAsync(RouteBinding, name));

                if (!mTrack.Key.ExternalName.IsNullOrWhiteSpace())
                {
                    await externalKeyLogic.DeleteExternalKeyAsync(mTrack.Key.ExternalName);
                }

                await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);

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

        private async Task<string> GetTrackNameAsync(string name = null, int count = 0)
        {
            if (name.IsNullOrWhiteSpace())
            {
                name = RandomGenerator.GenerateCode(Constants.ControlApi.DefaultNameLength).ToLower();
                if (count < 3)
                {
                    var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = name }, required: false);
                    if (mTrack != null)
                    {
                        count++;
                        return await GetTrackNameAsync(count: count);
                    }
                }
                return name;
            }
            else
            {
                return name.ToLower();
            }
        }
    }
}