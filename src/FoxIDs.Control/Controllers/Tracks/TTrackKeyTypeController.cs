using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using ITfoxtec.Identity;
using System.Collections.Generic;
using FoxIDs.Logic;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models.Config;
using Microsoft.Extensions.DependencyInjection;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TTrackKeyTypeController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly TrackCacheLogic trackCacheLogic;

        public TTrackKeyTypeController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IServiceProvider serviceProvider, IMapper mapper, ITenantDataRepository tenantDataRepository, TrackCacheLogic trackCacheLogic) : base(logger)
        {
            this.settings = settings;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.trackCacheLogic = trackCacheLogic;
        }

        /// <summary>
        /// Get environment key type.
        /// </summary>
        /// <returns>Track keys.</returns>
        [ProducesResponseType(typeof(Api.TrackKeyItemsContained), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackKey>> GetTrackKeyType()
        {
            try
            {
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName});
                return Ok(mapper.Map<Api.TrackKey>(mTrack.Key));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.TrackKey).Name}' type by environment name '{RouteBinding.TrackName}'.");
                    return NotFound(typeof(Api.TrackKey).Name, RouteBinding.TrackName);
                }
                throw;
            }
        }

        /// <summary>
        /// Update environment key type.
        /// </summary>
        /// <param name="trackKey">Track key.</param>
        /// <returns>Track keys.</returns>
        [ProducesResponseType(typeof(Api.TrackKey), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackKey>> PutTrackKeyType([FromBody] Api.TrackKey trackKey)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(trackKey)) return BadRequest(ModelState);

                var mTrackKey = mapper.Map<TrackKey>(trackKey);

                if (mTrackKey.Type == TrackKeyTypes.KeyVaultRenewSelfSigned)
                {
                    throw new Exception("KeyVault is phased out.");
                }

                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(trackIdKey);
                if (mTrack.Key.Type != mTrackKey.Type)
                {
                    if (mTrack.Key.Type == TrackKeyTypes.Contained || mTrack.Key.Type == TrackKeyTypes.ContainedRenewSelfSigned)
                    {
                        mTrack.Key.Type = mTrackKey.Type;
                        var certificate = mTrack.Key.Type == TrackKeyTypes.Contained ? await RouteBinding.CreateSelfSignedCertificateBySubjectAsync() : await RouteBinding.CreateSelfSignedCertificateBySubjectAsync(mTrack.KeyValidityInMonths);
                        mTrack.Key.Keys = new List<TrackKeyItem>
                        {
                            await certificate.ToTrackKeyItemAsync(true)
                        };

                        var externalName = mTrack.Key.ExternalName;
                        mTrack.Key.ExternalName = null;

                        await tenantDataRepository.UpdateAsync(mTrack);
                        await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);

                        if (settings.Options.KeyStorage == KeyStorageOptions.KeyVault && !externalName.IsNullOrWhiteSpace())
                        {
                            var externalKeyLogic = serviceProvider.GetService<ExternalKeyLogic>();
                            await externalKeyLogic.DeleteExternalKeyAsync(externalName);                            
                        }
                    }
                    else
                    {
                        throw new NotSupportedException($"Track key type '{mTrack.Key.Type}' not supported.");
                    }
                }

                return Ok(mapper.Map<Api.TrackKey>(mTrack.Key));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.TrackKey).Name}' type by environment name '{RouteBinding.TrackName}'.");
                    return NotFound(typeof(Api.TrackKey).Name, RouteBinding.TrackName);
                }
                throw;
            }
        }
    }
}
