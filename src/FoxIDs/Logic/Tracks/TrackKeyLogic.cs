using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using FoxIDs.Logic.Caches.Providers;

namespace FoxIDs.Logic
{
    public class TrackKeyLogic : LogicSequenceBase
    {
        private readonly ICacheProvider cacheProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly ExternalKeyLogic externalKeyLogic;

        public TrackKeyLogic(ICacheProvider cacheProvider, ITenantRepository tenantRepository, ExternalKeyLogic externalKeyLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.cacheProvider = cacheProvider;
            this.tenantRepository = tenantRepository;
            this.externalKeyLogic = externalKeyLogic;
        }

        public async Task<SecurityKey> GetPrimarySecurityKeyAsync(RouteTrackKey trackKey)
        {
            await ValidatePrimaryTrackKeyAsync(trackKey);

            switch (trackKey.Type)
            {
                case TrackKeyTypes.Contained:
                    return trackKey.PrimaryKey.Key.ToSecurityKey();

                case TrackKeyTypes.KeyVaultRenewSelfSigned:
                    return GetPrimaryRSAKeyVault(trackKey).ToSecurityKey(trackKey.PrimaryKey.Key.Kid);

                case TrackKeyTypes.KeyVaultImport:
                default:
                    throw new NotSupportedException($"Track primary key type '{trackKey.Type}' not supported.");
            }
        }

        public async Task<Saml2X509Certificate> GetPrimarySaml2X509CertificateAsync(RouteTrackKey trackKey)
        {
            await ValidatePrimaryTrackKeyAsync(trackKey);

            switch (trackKey.Type)
            {
                case TrackKeyTypes.Contained:
                    return trackKey.PrimaryKey.Key.ToSaml2X509Certificate(true);

                case TrackKeyTypes.KeyVaultRenewSelfSigned:
                    return new Saml2X509Certificate(trackKey.PrimaryKey.Key.ToX509Certificate(), GetPrimaryRSAKeyVault(trackKey));

                case TrackKeyTypes.KeyVaultImport:
                default:
                    throw new NotSupportedException($"Track primary key type '{trackKey.Type}' not supported.");
            }
        }

        public Saml2X509Certificate GetSecondarySaml2X509Certificate(RouteTrackKey trackKey)
        {
            try
            {
                if (trackKey.SecondaryKey != null && trackKey.SecondaryKey.Key != null)
                {
                    ValidateSecondaryTrackKey(trackKey);

                    switch (trackKey.Type)
                    {
                        case TrackKeyTypes.Contained:
                            return trackKey.SecondaryKey.Key.ToSaml2X509Certificate(true);

                        case TrackKeyTypes.KeyVaultRenewSelfSigned:
                            return new Saml2X509Certificate(trackKey.SecondaryKey.Key.ToX509Certificate(), GetPrimaryRSAKeyVault(trackKey));

                        case TrackKeyTypes.KeyVaultImport:
                        default:
                            throw new NotSupportedException($"Track secondary key type '{trackKey.Type}' not supported.");
                    }
                }
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetService<TelemetryScopedLogger>();
                logger.Warning(ex);
            }

            return null;
        }

        private RSA GetPrimaryRSAKeyVault(RouteTrackKey trackKey) => externalKeyLogic.GetExternalRSAKey(trackKey, trackKey.PrimaryKey);

        private async Task ValidatePrimaryTrackKeyAsync(RouteTrackKey trackKey)
        {
            var certificate = trackKey.PrimaryKey.Key.ToX509Certificate();
            try
            {
                certificate.ValidateCertificate("Track primary key");
            }
            catch (Exception ex)
            {
                if (RouteBinding.TrackName == Constants.Routes.MasterTrackName && RouteBinding.Key.Type != TrackKeyTypes.KeyVaultRenewSelfSigned)
                {
                    var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });

                    if (RouteBinding.Key.Type == TrackKeyTypes.Contained)
                    {
                        var ContainedCertificate = await RouteBinding.CreateSelfSignedCertificateBySubjectAsync();
                        mTrack.Key.Keys = new List<TrackKeyItem> { new TrackKeyItem { Key = await ContainedCertificate.ToFTJsonWebKeyAsync(true) } };
                        await tenantRepository.UpdateAsync(mTrack);

                        throw new ExternalKeyIsNotReadyException("The old primary master environment key certificate is invalid. A new primary environment key certificate has been created, please try one more time.", ex);
                    }
                    else
                    {
                        mTrack.Key.Type = TrackKeyTypes.KeyVaultRenewSelfSigned;
                        mTrack.Key.Keys = null;
                        mTrack.Key.ExternalName = await externalKeyLogic.CreateExternalKeyAsync(mTrack);
                        await tenantRepository.UpdateAsync(mTrack);

                        throw new ExternalKeyIsNotReadyException("The old primary master environment key certificate is invalid. A new primary external environment key certificate is under construction in Key Vault, it is ready in a little while.", ex);
                    }
                }

                throw;
            }
        }

        private void ValidateSecondaryTrackKey(RouteTrackKey trackKey)
        {
            var certificate = trackKey.SecondaryKey.Key.ToX509Certificate();
            certificate.ValidateCertificate("Track secondary key");
        }

        public async Task<RouteTrackKey> LoadTrackKeyAsync(TelemetryScopedLogger scopedLogger, Track.IdKey trackIdKey, Track track)
        {
            switch (track.Key.Type)
            {
                case TrackKeyTypes.Contained:
                    return new RouteTrackKey
                    {
                        Type = track.Key.Type,
                        PrimaryKey = new RouteTrackKeyItem { Key = track.Key.Keys[0].Key },
                        SecondaryKey = track.Key.Keys.Count > 1 ? new RouteTrackKeyItem { Key = track.Key.Keys[1].Key } : null,
                    };

                case TrackKeyTypes.KeyVaultRenewSelfSigned:
                    var trackKeyExternal = await GetTrackKeyItemsAsync(scopedLogger, trackIdKey.TenantName, trackIdKey.TrackName, track);
                    var externalRouteTrackKey = new RouteTrackKey
                    {
                        Type = track.Key.Type,
                        ExternalName = track.Key.ExternalName,
                    };
                    if (trackKeyExternal == null)
                    {
                        externalRouteTrackKey.PrimaryKey = new RouteTrackKeyItem { ExternalKeyIsNotReady = true };
                    }
                    else
                    {
                        externalRouteTrackKey.PrimaryKey = new RouteTrackKeyItem { ExternalId = trackKeyExternal.Keys[0].ExternalId, Key = trackKeyExternal.Keys[0].Key };
                        externalRouteTrackKey.SecondaryKey = trackKeyExternal.Keys.Count > 1 ? new RouteTrackKeyItem { ExternalId = trackKeyExternal.Keys[1].ExternalId, Key = trackKeyExternal.Keys[1].Key } : null;
                    }
                    return externalRouteTrackKey;

                case TrackKeyTypes.KeyVaultImport:
                default:
                    throw new Exception($"Track key type not supported '{track.Key.Type}'.");
            }
        }

        public async Task<TrackKeyExternal> GetTrackKeyItemsAsync(TelemetryScopedLogger scopedLogger, string tenantName, string trackName, Track track)
        {
            var key = RadisTrackKeyExternalKey(tenantName, trackName, track.Key.ExternalName);

            var trackKeyExternalValue = await cacheProvider.GetAsync(key);
            if (!trackKeyExternalValue.IsNullOrEmpty())
            {
                return trackKeyExternalValue.ToObject<TrackKeyExternal>();
            }

            var trackKeyExternal = await externalKeyLogic.LoadTrackKeyExternalFromKeyVaultAsync(scopedLogger, track);
            if (trackKeyExternal != null)
            {
                await cacheProvider.SetAsync(key, trackKeyExternal.ToJson(), track.KeyExternalCacheLifetime);
            }
            return trackKeyExternal;
        }

        private string RadisTrackKeyExternalKey(string tenantName, string trackName, string name)
        {
            return $"track_key_ext_{tenantName}_{trackName}_{name}";
        }
    }
}
