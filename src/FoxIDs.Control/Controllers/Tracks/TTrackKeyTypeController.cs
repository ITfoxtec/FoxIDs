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
using ITfoxtec.Identity;
using Azure.Core;
using Azure.Security.KeyVault.Certificates;
using FoxIDs.Models.Config;
using System.Collections.Generic;
using ITfoxtec.Identity.Util;

namespace FoxIDs.Controllers
{
    public class TTrackKeyTypeController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly FoxIDsControlSettings settings;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;
        private readonly TokenCredential tokenCredential;

        public TTrackKeyTypeController(TelemetryScopedLogger logger, FoxIDsControlSettings settings, IMapper mapper, ITenantRepository tenantRepository, TokenCredential tokenCredential) : base(logger)
        {
            this.logger = logger;
            this.settings = settings;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
            this.tokenCredential = tokenCredential;
        }

        /// <summary>
        /// Get track key type.
        /// </summary>
        /// <returns>Track keys.</returns>
        [ProducesResponseType(typeof(Api.TrackKeyItemsContained), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.TrackKey>> GetTrackKeyType()
        {
            try
            {
                var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName});
                return Ok(mapper.Map<Api.TrackKey>(mTrack.Key));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.TrackKey).Name}' type by track name '{RouteBinding.TrackName}'.");
                    return NotFound(typeof(Api.TrackKey).Name, RouteBinding.TrackName);
                }
                throw;
            }
        }

        /// <summary>
        /// Update track key type.
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

                var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });
                if (mTrack.Key.Type != mTrackKey.Type)
                {
                    switch (mTrackKey.Type)
                    {
                        case TrackKeyType.Contained:
                            mTrack.Key.Type = mTrackKey.Type;
                            var certificate = await mTrack.Name.CreateSelfSignedCertificateByCnAsync();
                            mTrack.Key.Keys = new List<TrackKeyItem> { new TrackKeyItem { Key = await certificate.ToFTJsonWebKeyAsync(true) } };
                            if (!mTrack.Key.ExternalName.IsNullOrWhiteSpace())
                            {
                                await DeleteExternalKeyAsync(mTrack.Key.ExternalName);
                                mTrack.Key.ExternalName = null;
                            }
                            break;

                        case TrackKeyType.KeyVaultRenewSelfSigned:
                            mTrack.Key.Type = mTrackKey.Type;
                            mTrack.Key.Keys = null;
                            if (mTrack.Key.ExternalName.IsNullOrWhiteSpace())
                            {
                                mTrack.Key.ExternalName = await CreateExternalKeyAsync(mTrack);
                            }
                            break;

                        case TrackKeyType.KeyVaultUpload:
                        default:
                            throw new Exception($"Track key type not supported '{mTrackKey.Type}'.");
                    }

                    await tenantRepository.UpdateAsync(mTrack);
                }

                return Ok(mapper.Map<Api.TrackKey>(mTrack.Key));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.TrackKey).Name}' type by track name '{RouteBinding.TrackName}'.");
                    return NotFound(typeof(Api.TrackKey).Name, RouteBinding.TrackName);
                }
                throw;
            }
        }

        private async Task<string> CreateExternalKeyAsync(Track mTrack)
        {
            var certificateClient = new CertificateClient(new Uri(settings.KeyVault.EndpointUri), tokenCredential);

            var externalName = $"{RouteBinding.TenantDashTrackName}-{Guid.NewGuid()}";
            var cn = RouteBinding.TenantDashTrackName;
            var certificatePolicy = new CertificatePolicy("self", $"CN={cn}, O=FoxIDs")
            {
                Exportable = false,
                ValidityInMonths = mTrack.KeyExternalValidityInMonths
            };
            certificatePolicy.LifetimeActions.Add(new LifetimeAction(CertificatePolicyAction.AutoRenew)
            {
                DaysBeforeExpiry = mTrack.KeyExternalAutoRenewDaysBeforeExpiry
            });            
            await certificateClient.StartCreateCertificateAsync(externalName, certificatePolicy);
            return externalName;
        }

        private async Task DeleteExternalKeyAsync(string externalName)
        {
            var certificateClient = new CertificateClient(new Uri(settings.KeyVault.EndpointUri), tokenCredential);
            await certificateClient.StartDeleteCertificateAsync(externalName);
        }
    }
}
