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
using ITfoxtec.Identity;
using System.Collections.Generic;
using FoxIDs.Logic;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models.Config;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public class TTrackKeyTypeController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly PlanCacheLogic planCacheLogic;
        private readonly TrackCacheLogic trackCacheLogic;
        private readonly ExternalKeyLogic externalKeyLogic;

        public TTrackKeyTypeController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, PlanCacheLogic planCacheLogic, TrackCacheLogic trackCacheLogic, ExternalKeyLogic externalKeyLogic) : base(logger)
        {
            this.settings = settings;
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.planCacheLogic = planCacheLogic;
            this.trackCacheLogic = trackCacheLogic;
            this.externalKeyLogic = externalKeyLogic;
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

                if (settings.Options.KeyStorage != KeyStorageOptions.KeyVault && (mTrackKey.Type == TrackKeyTypes.KeyVaultRenewSelfSigned || mTrackKey.Type == TrackKeyTypes.KeyVaultImport))
                {
                    throw new Exception("KeyVault option not enabled.");
                }

                if (!RouteBinding.PlanName.IsNullOrEmpty() && mTrackKey.Type != TrackKeyTypes.Contained)
                {
                    var plan = await planCacheLogic.GetPlanAsync(RouteBinding.PlanName);
                    if (!plan.EnableKeyVault)
                    {
                        throw new Exception($"Key Vault is not supported in the '{plan.Name}' plan.");
                    }
                }

                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(trackIdKey);
                if (mTrack.Key.Type != mTrackKey.Type)
                {
                    switch (mTrackKey.Type)
                    {
                        case TrackKeyTypes.Contained:
                            mTrack.Key.Type = mTrackKey.Type;
                            var certificate = await RouteBinding.CreateSelfSignedCertificateBySubjectAsync();
                            mTrack.Key.Keys = new List<TrackKeyItem> { new TrackKeyItem { Key = await certificate.ToFTJsonWebKeyAsync(true) } };
                            if (!mTrack.Key.ExternalName.IsNullOrWhiteSpace())
                            {
                                await externalKeyLogic.DeleteExternalKeyAsync(mTrack.Key.ExternalName);
                                mTrack.Key.ExternalName = null;
                            }
                            break;

                        case TrackKeyTypes.KeyVaultRenewSelfSigned:
                            mTrack.Key.Type = mTrackKey.Type;
                            mTrack.Key.Keys = null;
                            mTrack.Key.ExternalName = await externalKeyLogic.CreateExternalKeyAsync(mTrack);
                            break;

                        case TrackKeyTypes.KeyVaultImport:
                        default:
                            throw new Exception($"Track key type not supported '{mTrackKey.Type}'.");
                    }

                    await tenantDataRepository.UpdateAsync(mTrack);

                    await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);
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
