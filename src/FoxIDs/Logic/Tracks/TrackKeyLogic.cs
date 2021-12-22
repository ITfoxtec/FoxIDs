using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Azure.Core;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using RSAKeyVaultProvider;
using UrlCombineLib;

namespace FoxIDs.Logic
{
    public class TrackKeyLogic : LogicBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TokenCredential tokenCredential;
        private readonly ITenantRepository tenantRepository;
        private readonly ExternalKeyLogic externalKeyLogic;

        public TrackKeyLogic(FoxIDsSettings settings, TokenCredential tokenCredential, ITenantRepository tenantRepository, ExternalKeyLogic externalKeyLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
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
                    return new RsaSecurityKey(GetRSAKeyVault(trackKey));

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
                    return new Saml2X509Certificate(trackKey.PrimaryKey.Key.ToX509Certificate(), GetRSAKeyVault(trackKey));

                case TrackKeyType.KeyVaultUpload:
                default:
                    throw new NotSupportedException($"Track primary key type '{trackKey.Type}' not supported.");
            }
        }

        public Saml2X509Certificate GetSecondarySaml2X509Certificate(RouteTrackKey trackKey)
        {
            if (trackKey.SecondaryKey == null || trackKey.SecondaryKey.Key == null)
            {
                return null;
            }

            ValidateSecondaryTrackKey(trackKey);

            switch (trackKey.Type)
            {
                case TrackKeyType.Contained:
                    return trackKey.SecondaryKey.Key.ToSaml2X509Certificate(true);

                case TrackKeyType.KeyVaultRenewSelfSigned:
                    return new Saml2X509Certificate(trackKey.SecondaryKey.Key.ToX509Certificate(), GetRSAKeyVault(trackKey));

                case TrackKeyType.KeyVaultUpload:
                default:
                    throw new NotSupportedException($"Track secondary key type '{trackKey.Type}' not supported.");
            }
        }

        private RSA GetRSAKeyVault(RouteTrackKey trackKey)
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
                    throw new Exception($"Track primary key certificate not valid yet. Not before {certificate.NotBefore.ToUniversalTime():u}.");
                }
                if (certificate.NotAfter < nowLocal)
                {
                    throw new Exception($"Track primary key certificate has expired. Not after {certificate.NotAfter.ToUniversalTime():u}.");
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

                    throw new ExternalKeyIsNotReadyException("Primary external track key certificate is probably not ready in Key Vault.", ex);
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
                throw new Exception($"Track secondary key certificate not valid yet. Not before {certificate.NotBefore.ToUniversalTime():u}.");
            }
            if (certificate.NotAfter < nowLocal)
            {
                throw new Exception($"Track secondary key certificate has expired. Not after {certificate.NotAfter.ToUniversalTime():u}.");
            }
        }
    }
}
