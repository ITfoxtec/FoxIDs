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
using System.Security.Cryptography.X509Certificates;

namespace FoxIDs.Logic
{
    public class TrackKeyLogic : LogicSequenceBase
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly TrackCacheLogic trackCacheLogic;

        public TrackKeyLogic(IServiceProvider serviceProvider, ITenantDataRepository tenantDataRepository, TrackCacheLogic trackCacheLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.serviceProvider = serviceProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.trackCacheLogic = trackCacheLogic;
        }

        public async Task<SecurityKey> GetPrimarySecurityKeyAsync(RouteTrackKey trackKey)
        {
            await ValidatePrimaryTrackKeyAsync(trackKey);

            switch (trackKey.Type)
            {
                case TrackKeyTypes.Contained:
                case TrackKeyTypes.ContainedRenewSelfSigned:
                    return trackKey.PrimaryKey.Key.ToSecurityKey();

                case TrackKeyTypes.KeyVaultRenewSelfSigned:
                    return GetPrimaryRSAKeyVault(trackKey).ToSecurityKey(trackKey.PrimaryKey.Key.Kid);

                default:
                    throw new NotSupportedException($"Track primary key type '{trackKey.Type}' not supported.");
            }
        }

        public async Task<X509Certificate2> GetPrimaryMtlsX509CertificateAsync(RouteTrackKey trackKey)
        {
            await ValidatePrimaryTrackKeyAsync(trackKey);

            switch (trackKey.Type)
            {
                case TrackKeyTypes.Contained:
                case TrackKeyTypes.ContainedRenewSelfSigned:
                    return trackKey.PrimaryKey.Key.ToX509Certificate(includePrivateKey: true);

                case TrackKeyTypes.KeyVaultRenewSelfSigned:
                    throw new NotSupportedException("Track key vault renew self signed does not support mTLS client authentication certificates.");

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
                case TrackKeyTypes.ContainedRenewSelfSigned:
                    return trackKey.PrimaryKey.Key.ToSaml2X509Certificate(true);

                case TrackKeyTypes.KeyVaultRenewSelfSigned:
                    return new Saml2X509Certificate(trackKey.PrimaryKey.Key.ToX509Certificate(), GetPrimaryRSAKeyVault(trackKey));

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
                        case TrackKeyTypes.ContainedRenewSelfSigned:
                            return trackKey.SecondaryKey.Key.ToSaml2X509Certificate(true);

                        case TrackKeyTypes.KeyVaultRenewSelfSigned:
                            return new Saml2X509Certificate(trackKey.SecondaryKey.Key.ToX509Certificate(), GetPrimaryRSAKeyVault(trackKey));

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

        private RSA GetPrimaryRSAKeyVault(RouteTrackKey trackKey) => GetExternalKeyLogic().GetExternalRSAKey(trackKey, trackKey.PrimaryKey);

        private async Task ValidatePrimaryTrackKeyAsync(RouteTrackKey trackKey)
        {
            var certificate = trackKey.PrimaryKey.Key.ToX509Certificate();
            try
            {
                certificate.ValidateCertificate("Track primary key");
            }
            catch (Exception ex)
            {
                if (RouteBinding.Key.Type != TrackKeyTypes.KeyVaultRenewSelfSigned)
                {
                    var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                    var mTrack = await tenantDataRepository.GetTrackByNameAsync(trackIdKey);

                    var newCertificate = mTrack.Key.Type == TrackKeyTypes.Contained ? await RouteBinding.CreateSelfSignedCertificateBySubjectAsync() : await RouteBinding.CreateSelfSignedCertificateBySubjectAsync(mTrack.KeyValidityInMonths);
                    mTrack.Key.Keys = new List<TrackKeyItem>
                    {
                        await newCertificate.ToTrackKeyItemAsync(true)
                    };
                    await tenantDataRepository.UpdateAsync(mTrack);

                    await trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey);

                    throw new ExternalKeyIsNotReadyException("The old primary environment key certificate is invalid. A new primary environment key certificate has been created, please try one more time.", ex);
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
                case TrackKeyTypes.ContainedRenewSelfSigned:
                    return new RouteTrackKey
                    {
                        Type = track.Key.Type,
                        PrimaryKey = new RouteTrackKeyItem { Key = track.Key.Keys[0].Key },
                        SecondaryKey = track.Key.Keys.Count > 1 ? new RouteTrackKeyItem { Key = track.Key.Keys[1].Key } : null,
                    };

                case TrackKeyTypes.KeyVaultRenewSelfSigned:
                    var trackKeyExternal = await GetExternalKeyLogic().GetTrackKeyItemsAsync(scopedLogger, trackIdKey.TenantName, trackIdKey.TrackName, track);
                    var externalRouteTrackKey = new RouteTrackKey
                    {
                        Type = track.Key.Type,
                        ExternalName = track.Key.ExternalName,
                    };
                    if (trackKeyExternal == null)
                    {
                        if (track.Key.Keys != null)
                        {
                            externalRouteTrackKey.PrimaryKey = new RouteTrackKeyItem { Key = track.Key.Keys[0].Key };
                        }
                        else
                        {
                            externalRouteTrackKey.PrimaryKey = new RouteTrackKeyItem { ExternalKeyIsNotReady = true };
                        }
                    }
                    else
                    {
                        externalRouteTrackKey.PrimaryKey = new RouteTrackKeyItem { ExternalId = trackKeyExternal.Keys[0].ExternalId, Key = trackKeyExternal.Keys[0].Key };
                        if (track.Key.Keys != null)
                        {
                            externalRouteTrackKey.SecondaryKey = new RouteTrackKeyItem { Key = track.Key.Keys[0].Key };
                        }
                        else
                        {
                            externalRouteTrackKey.SecondaryKey = trackKeyExternal.Keys.Count > 1 ? new RouteTrackKeyItem { ExternalId = trackKeyExternal.Keys[1].ExternalId, Key = trackKeyExternal.Keys[1].Key } : null;
                        }
                    }

                    return externalRouteTrackKey;

                default:
                    throw new Exception($"Track key type not supported '{track.Key.Type}'.");
            }
        }

        private ExternalKeyLogic GetExternalKeyLogic() => serviceProvider.GetService<ExternalKeyLogic>();
    }
}
