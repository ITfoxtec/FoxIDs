using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using Azure.Core;
using Azure.Security.KeyVault.Certificates;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using System.Security.Cryptography;
using RSAKeyVaultProvider;
using ITfoxtec.Identity.Models;
using FoxIDs.Infrastructure;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Linq;

namespace FoxIDs.Logic
{
    public class ExternalKeyLogic : LogicBase
    {
        private readonly Settings settings;
        private readonly TokenCredential tokenCredential;

        public ExternalKeyLogic(Settings settings, TokenCredential tokenCredential, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.tokenCredential = tokenCredential;
        }

        public async Task<string> CreateExternalKeyAsync(Track mTrack, string tenantName = null, string trackName = null, string upPartyName = null, bool autoRenew = true, int? ValidityInMonths = null)
        {
            tenantName = tenantName ?? RouteBinding.TenantName;
            trackName = trackName ?? RouteBinding.TrackName;
            var externalName = $"{tenantName}-{trackName}-{(upPartyName.IsNullOrEmpty() ? string.Empty : $"UP{upPartyName}-")}{Guid.NewGuid()}";
            externalName = externalName.Replace('_', 'U');

            var certificatePolicy = new CertificatePolicy("self", (tenantName, trackName).GetCertificateSubject())
            {
                Exportable = false,
                ValidityInMonths = ValidityInMonths ?? mTrack.KeyExternalValidityInMonths
            };
            certificatePolicy.KeyUsage.Add(CertificateKeyUsage.DigitalSignature);
            certificatePolicy.KeyUsage.Add(CertificateKeyUsage.KeyEncipherment);
            certificatePolicy.KeyUsage.Add(CertificateKeyUsage.DataEncipherment);
            if (autoRenew)
            {
                certificatePolicy.LifetimeActions.Add(new LifetimeAction(CertificatePolicyAction.AutoRenew)
                {
                    DaysBeforeExpiry = mTrack.KeyExternalAutoRenewDaysBeforeExpiry
                });
            }
            var certificateClient = new CertificateClient(new Uri(settings.KeyVault.EndpointUri), tokenCredential);
            var response = await certificateClient.StartCreateCertificateAsync(externalName, certificatePolicy);

            return externalName;
        }

        public async Task<(string externalName, byte[] publicCertificate, string externalId)> ImportExternalKeyAsync(byte[] certificate, string password, string tenantName = null, string trackName = null, string upPartyName = null)
        {
            tenantName = tenantName ?? RouteBinding.TenantName;
            trackName = trackName ?? RouteBinding.TrackName;
            var externalName = $"{tenantName}-{trackName}-{(upPartyName.IsNullOrEmpty() ? string.Empty : $"UP{upPartyName}-")}{Guid.NewGuid()}";
            externalName = externalName.Replace('_', 'U');

            var importCertificateOptions = new ImportCertificateOptions(externalName, certificate)
            {
                Enabled = true,
                Password = password
            };

            var certificateClient = new CertificateClient(new Uri(settings.KeyVault.EndpointUri), tokenCredential);
            var response = await certificateClient.ImportCertificateAsync(importCertificateOptions);

            return (externalName, response.Value.Cer, response.Value.Properties.Version);
        }

        public async Task DeleteExternalKeyAsync(string externalName)
        {
            var certificateClient = new CertificateClient(new Uri(settings.KeyVault.EndpointUri), tokenCredential);
            await certificateClient.StartDeleteCertificateAsync(externalName);
        }

        public RSA GetExternalRSAKey(ClientKey clientKey)
        {
            return GetExternalRSAKey(clientKey.ExternalName, clientKey.ExternalId, clientKey.PublicKey);
        }

        public RSA GetExternalRSAKey(RouteTrackKey trackKey, RouteTrackKeyItem keyItem)
        {
            return GetExternalRSAKey(trackKey.ExternalName, keyItem.ExternalId, keyItem.Key);
        }

        private RSA GetExternalRSAKey(string externalName, string externalId, JsonWebKey publicKey)
        {
            return RSAFactory.Create(tokenCredential, new Uri(UrlCombine.Combine(settings.KeyVault.EndpointUri, "keys", externalName, externalId)), new Azure.Security.KeyVault.Keys.JsonWebKey(publicKey.ToRsa()));
        }

        public async Task<TrackKeyExternal> LoadTrackKeyExternalFromKeyVaultAsync(TelemetryScopedLogger scopedLogger, Track track)
        {
            var now = DateTimeOffset.UtcNow;
            var certificateClient = new CertificateClient(new Uri(settings.KeyVault.EndpointUri), tokenCredential);

            var externalKeys = new List<(string Id, DateTimeOffset? NotBefore)>();
            var certificateVersions = certificateClient.GetPropertiesOfCertificateVersionsAsync(track.Key.ExternalName);
            await foreach (var certificateVersion in certificateVersions)
            {
                if (certificateVersion.Enabled == true && certificateVersion.ExpiresOn >= now)
                {
                    externalKeys.Add((certificateVersion.Version, certificateVersion.NotBefore));
                }
            }

            if (externalKeys.Count <= 0)
            {
                try
                {
                    throw new Exception($"Track key external certificate '{track.Key.ExternalName}' do not exist in Key Vault, probably because it is not ready in Key Vault.");
                }
                catch (Exception ex)
                {
                    scopedLogger.Warning(ex);
                    return null;
                }
            }

            var trackKeyExternal = new TrackKeyExternal();
            if (externalKeys.Count == 1)
            {
                trackKeyExternal.Keys = new List<TrackKeyExternalItem> { new TrackKeyExternalItem { ExternalId = externalKeys.First().Id } };
            }
            else
            {
                var topTwoExternalKeys = externalKeys.OrderByDescending(e => e.NotBefore).Take(2);
                var firstExternalKey = topTwoExternalKeys.First();
                if (firstExternalKey.NotBefore <= now.AddDays(-track.KeyExternalPrimaryAfterDays))
                {
                    trackKeyExternal.Keys = new List<TrackKeyExternalItem> { new TrackKeyExternalItem { ExternalId = firstExternalKey.Id }, new TrackKeyExternalItem { ExternalId = topTwoExternalKeys.Last().Id } };
                }
                else
                {
                    trackKeyExternal.Keys = new List<TrackKeyExternalItem> { new TrackKeyExternalItem { ExternalId = topTwoExternalKeys.Last().Id }, new TrackKeyExternalItem { ExternalId = firstExternalKey.Id } };
                }
            }

            foreach (var keyItem in trackKeyExternal.Keys)
            {
                var certificateWithPolicy = certificateClient.GetCertificateVersion(track.Key.ExternalName, keyItem.ExternalId);
                var certificateRawValue = certificateWithPolicy?.Value?.Cer;
                if (certificateRawValue == null)
                {
                    throw new Exception($"Track key external certificate '{track.Key.ExternalName}' version '{keyItem.ExternalId}' from Key Vault is null.");
                }
                keyItem.Key = await new X509Certificate2(certificateRawValue).ToFTJsonWebKeyAsync();
            }
            return trackKeyExternal;
        }
    }
}
