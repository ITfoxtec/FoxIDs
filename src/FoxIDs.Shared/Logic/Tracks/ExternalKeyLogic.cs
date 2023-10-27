using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using Azure.Core;
using Azure.Security.KeyVault.Certificates;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;

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
    }
}
