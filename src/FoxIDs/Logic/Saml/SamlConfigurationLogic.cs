using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using UrlCombineLib;

namespace FoxIDs.Logic
{
    public class Saml2ConfigurationLogic : LogicBase
    {
        private readonly TrackKeyLogic trackKeyLogic;

        public Saml2ConfigurationLogic(TrackKeyLogic trackKeyLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.trackKeyLogic = trackKeyLogic;
        }

        public Saml2Configuration GetSamlUpConfig(SamlUpParty party, bool includeSigningCertificate = false)
        {
            var samlConfig = new Saml2Configuration();
            if (!party.IdSIssuer.IsNullOrEmpty())
            {
                samlConfig.Issuer = party.IdSIssuer;
            }
            else
            {
                samlConfig.Issuer = UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName);
            }

            samlConfig.AllowedAudienceUris.Add(samlConfig.Issuer);

            samlConfig.SingleSignOnDestination = new Uri(party.AuthnUrl);
            if(!party.LogoutUrl.IsNullOrEmpty())
            {
                samlConfig.SingleLogoutDestination = new Uri(party.LogoutUrl);
            }

            foreach (var key in party.Keys)
            {
                samlConfig.SignatureValidationCertificates.Add(key.ToSaml2X509Certificate());
            }

            if (includeSigningCertificate)
            {
                samlConfig.SigningCertificate = trackKeyLogic.GetPrimarySaml2X509Certificate(RouteBinding.Key);
            }
            samlConfig.SignatureAlgorithm = party.SignatureAlgorithm;

            samlConfig.CertificateValidationMode = party.CertificateValidationMode;
            samlConfig.RevocationMode = party.RevocationMode;

            return samlConfig;
        }

        public Saml2Configuration GetSamlDownConfig(SamlDownParty party, bool includeSigningCertificate = false)
        {
            var samlConfig = new Saml2Configuration();
            if (!party.IdSIssuer.IsNullOrEmpty())
            {
                samlConfig.Issuer = party.IdSIssuer;
            }
            else
            {
                samlConfig.Issuer = UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName);
            }

            if (party.Keys?.Count > 0)
            {
                foreach (var key in party.Keys)
                {
                    samlConfig.SignatureValidationCertificates.Add(key.ToSaml2X509Certificate());
                }
            }

            if (includeSigningCertificate)
            {
                samlConfig.SigningCertificate = trackKeyLogic.GetPrimarySaml2X509Certificate(RouteBinding.Key);
            }
            samlConfig.SignatureAlgorithm = party.SignatureAlgorithm;

            samlConfig.CertificateValidationMode = party.CertificateValidationMode;
            samlConfig.RevocationMode = party.RevocationMode;

            return samlConfig;
        }

    }
}
