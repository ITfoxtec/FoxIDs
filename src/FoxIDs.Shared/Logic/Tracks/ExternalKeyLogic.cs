﻿using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using Azure.Core;
using Azure.Security.KeyVault.Certificates;
using FoxIDs.Models.Config;

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

        public async Task<string> CreateExternalKeyAsync(Track mTrack, string tenantName = null)
        {
            var certificateClient = new CertificateClient(new Uri(settings.KeyVault.EndpointUri), tokenCredential);

            tenantName = tenantName ?? RouteBinding.TenantName;
            var externalName = $"{tenantName}-{mTrack.Name}-{Guid.NewGuid()}";
            var certificatePolicy = new CertificatePolicy("self", RouteBinding.GetCertificateSubject())
            {
                Exportable = false,
                ValidityInMonths = mTrack.KeyExternalValidityInMonths
            };
            certificatePolicy.KeyUsage.Add(CertificateKeyUsage.DigitalSignature);
            certificatePolicy.KeyUsage.Add(CertificateKeyUsage.KeyEncipherment);
            certificatePolicy.KeyUsage.Add(CertificateKeyUsage.DataEncipherment);
            certificatePolicy.LifetimeActions.Add(new LifetimeAction(CertificatePolicyAction.AutoRenew)
            {
                DaysBeforeExpiry = mTrack.KeyExternalAutoRenewDaysBeforeExpiry
            });
            await certificateClient.StartCreateCertificateAsync(externalName, certificatePolicy);
            return externalName;
        }

        public async Task DeleteExternalKeyAsync(string externalName)
        {
            var certificateClient = new CertificateClient(new Uri(settings.KeyVault.EndpointUri), tokenCredential);
            await certificateClient.StartDeleteCertificateAsync(externalName);
        }
    }
}