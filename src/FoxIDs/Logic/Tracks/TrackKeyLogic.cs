using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Azure.Core;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RSAKeyVaultProvider;
using ITfoxtec.Identity.Util;

namespace FoxIDs.Logic
{
    public class TrackKeyLogic : LogicSequenceBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly TokenCredential tokenCredential;
        private readonly ITenantRepository tenantRepository;
        private readonly ExternalKeyLogic externalKeyLogic;

        public TrackKeyLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, TokenCredential tokenCredential, ITenantRepository tenantRepository, ExternalKeyLogic externalKeyLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.tokenCredential = tokenCredential;
            this.tenantRepository = tenantRepository;
            this.externalKeyLogic = externalKeyLogic;
        }

        public async Task<SecurityKey> GetPrimarySecurityKeyAsync(RouteTrackKey trackKey)
        {
            await ValidatePrimaryTrackKeyAsync(trackKey);

            switch (trackKey.Type)
            {
                case TrackKeyType.Contained:
                    return trackKey.PrimaryKey.Key.ToSecurityKey();

                case TrackKeyType.KeyVaultRenewSelfSigned:
                    return GetPrimaryRSAKeyVault(trackKey).ToSecurityKey(trackKey.PrimaryKey.Key.Kid);

                case TrackKeyType.KeyVaultUpload:
                default:
                    throw new NotSupportedException($"Track primary key type '{trackKey.Type}' not supported.");
            }
        }

        public async Task<Saml2X509Certificate> GetPrimarySaml2X509CertificateAsync(RouteTrackKey trackKey)
        {
            await ValidatePrimaryTrackKeyAsync(trackKey);

            switch (trackKey.Type)
            {
                case TrackKeyType.Contained:
                    return trackKey.PrimaryKey.Key.ToSaml2X509Certificate(true);

                case TrackKeyType.KeyVaultRenewSelfSigned:
                    return new Saml2X509Certificate(trackKey.PrimaryKey.Key.ToX509Certificate(), GetPrimaryRSAKeyVault(trackKey));

                case TrackKeyType.KeyVaultUpload:
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
                        case TrackKeyType.Contained:
                            return trackKey.SecondaryKey.Key.ToSaml2X509Certificate(true);

                        case TrackKeyType.KeyVaultRenewSelfSigned:
                            return new Saml2X509Certificate(trackKey.SecondaryKey.Key.ToX509Certificate(), GetPrimaryRSAKeyVault(trackKey));

                        case TrackKeyType.KeyVaultUpload:
                        default:
                            throw new NotSupportedException($"Track secondary key type '{trackKey.Type}' not supported.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex);
            }

            return null;
        }

        private RSA GetPrimaryRSAKeyVault(RouteTrackKey trackKey)
        {
            return RSAFactory.Create(tokenCredential, new Uri(UrlCombine.Combine(settings.KeyVault.EndpointUri, "keys", trackKey.ExternalName, trackKey.PrimaryKey.ExternalId)), new Azure.Security.KeyVault.Keys.JsonWebKey(trackKey.PrimaryKey.Key.ToRsa()));
        }

        private async Task ValidatePrimaryTrackKeyAsync(RouteTrackKey trackKey)
        {
            var nowLocal = DateTime.Now;
            var certificate = trackKey.PrimaryKey.Key.ToX509Certificate();
            try
            {
                if (certificate.NotBefore > nowLocal)
                {
                    throw new KeyException($"Track primary key certificate not valid yet. Not before {certificate.NotBefore.ToUniversalTime():u}.");
                }
                if (certificate.NotAfter < nowLocal)
                {
                    throw new KeyException($"Track primary key certificate has expired. Not after {certificate.NotAfter.ToUniversalTime():u}.");
                }
            }
            catch (Exception ex)
            {
                if (RouteBinding.TrackName == Constants.Routes.MasterTrackName && RouteBinding.Key.Type != TrackKeyType.KeyVaultRenewSelfSigned)
                {
                    var mTrack = await tenantRepository.GetTrackByNameAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });
                    mTrack.Key.Type = TrackKeyType.KeyVaultRenewSelfSigned;
                    mTrack.Key.Keys = null;
                    mTrack.Key.ExternalName = await externalKeyLogic.CreateExternalKeyAsync(mTrack);
                    await tenantRepository.UpdateAsync(mTrack);

                    throw new ExternalKeyIsNotReadyException("The old primary master track key certificate is invalid. A new primary external track key certificate is under construction in Key Vault, it is ready in a little while.", ex);
                }

                throw;
            }
        }

        private void ValidateSecondaryTrackKey(RouteTrackKey trackKey)
        {
            var nowLocal = DateTime.Now;
            var certificate = trackKey.SecondaryKey.Key.ToX509Certificate();
            if (certificate.NotBefore > nowLocal)
            {
                throw new KeyException($"Track secondary key certificate not valid yet. Not before {certificate.NotBefore.ToUniversalTime():u}.");
            }
            if (certificate.NotAfter < nowLocal)
            {
                throw new KeyException($"Track secondary key certificate has expired. Not after {certificate.NotAfter.ToUniversalTime():u}.");
            }
        }
    }
}
