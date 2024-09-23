using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Logic;
using ITfoxtec.Identity;
using System;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models.Config;
using Microsoft.Extensions.DependencyInjection;
using FoxIDs.Util;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TTrackController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly PlanCacheLogic planCacheLogic;
        private readonly TrackCacheLogic trackCacheLogic;
        private readonly TrackLogic trackLogic;

        public TTrackController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IServiceProvider serviceProvider, IMapper mapper, ITenantDataRepository tenantDataRepository, PlanCacheLogic planCacheLogic, TrackCacheLogic trackCacheLogic, TrackLogic trackLogic) : base(logger)
        {
            this.settings = settings;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.planCacheLogic = planCacheLogic;
            this.trackCacheLogic = trackCacheLogic;
            this.trackLogic = trackLogic;
        }

        /// <summary>
        /// Get environment.
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

                var mTrack = await tenantDataRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = name});
                return Ok(mapper.Map<Api.Track>(mTrack));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.Track).Name}' by name '{name}'.");
                    return NotFound(typeof(Api.Track).Name, name);
                }
                throw;
            }
        }

        /// <summary>
        /// Create environment.
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

                if (track.Name == Constants.Routes.ControlSiteName || track.Name == Constants.Routes.HealthController)
                {
                    throw new FoxIDsDataException($"A track can not have the name '{track.Name}'.") { StatusCode = DataStatusCode.Conflict };
                }

                if (!RouteBinding.PlanName.IsNullOrEmpty())
                {
                    var plan = await planCacheLogic.GetPlanAsync(RouteBinding.PlanName);
                    if (plan.Tracks.IsLimited)
                    {
                        var count = await tenantDataRepository.CountAsync<Track>(new Track.IdKey { TenantName = RouteBinding.TenantName });
                        // included + master track
                        if (count > plan.Tracks.Included) 
                        {
                            throw new Exception($"Maximum number of tracks ({plan.Tracks.Included}) included in the '{plan.Name}' plan has been reached. Master environment not counted.");
                        }
                    }
                }

                var mTrack = mapper.Map<Track>(track);
                await trackLogic.CreateTrackDocumentAsync(mTrack);
                await trackLogic.CreateLoginDocumentAsync(mTrack);

                return Created(mapper.Map<Api.Track>(mTrack));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create '{typeof(Api.Track).Name}' by name '{track.Name}'.");
                    return Conflict(typeof(Api.Track).Name, track.Name, nameof(track.Name));
                }
                throw;
            }
        }

        /// <summary>
        /// Update environment.
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
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(trackIdKey);
                mTrack.DisplayName = track.DisplayName;
                mTrack.SequenceLifetime = track.SequenceLifetime;
                mTrack.AutoMapSamlClaims = track.AutoMapSamlClaims;
                mTrack.MaxFailingLogins = track.MaxFailingLogins;
                mTrack.FailingLoginCountLifetime = track.FailingLoginCountLifetime;
                mTrack.FailingLoginObservationPeriod = track.FailingLoginObservationPeriod;
                mTrack.PasswordLength = track.PasswordLength;
                mTrack.CheckPasswordComplexity = track.CheckPasswordComplexity;
                mTrack.CheckPasswordRisk = track.CheckPasswordRisk;
                mTrack.AllowIframeOnDomains = track.AllowIframeOnDomains;
                await tenantDataRepository.UpdateAsync(mTrack);

                await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);

                return Ok(mapper.Map<Api.Track>(mTrack));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.Track).Name}' by name '{track.Name}'.");
                    return NotFound(typeof(Api.Track).Name, track.Name, nameof(track.Name));
                }
                throw;
            }
        }

        /// <summary>
        /// Delete environment.
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
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(trackIdKey);

                await tenantDataRepository.DeleteListAsync<DefaultElement>(trackIdKey);
                await tenantDataRepository.DeleteAsync<Track>(await Track.IdFormatAsync(RouteBinding, name));

                if (settings.Options.KeyStorage == KeyStorageOptions.KeyVault && !mTrack.Key.ExternalName.IsNullOrWhiteSpace())
                {
                    await serviceProvider.GetService<ExternalKeyLogic>().DeleteExternalKeyAsync(mTrack.Key.ExternalName);
                }

                await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
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
                name = RandomName.GenerateDefaultName();
                if (count < Constants.Models.DefaultNameMaxAttempts)
                {
                    var mTrack = await tenantDataRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = name }, required: false);
                    if (mTrack != null)
                    {
                        count++;
                        return await GetTrackNameAsync(count: count);
                    }
                }
            }
            return name;
        }
    }
}