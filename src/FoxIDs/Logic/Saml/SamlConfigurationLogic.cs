using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System;
using UrlCombineLib;

namespace FoxIDs.Logic
{
    public class Saml2ConfigurationLogic : LogicBase
    {
        private readonly TrackKeyLogic trackKeyLogic;
        private readonly TrackIssuerLogic trackIssuerLogic;

        public Saml2ConfigurationLogic(TrackKeyLogic trackKeyLogic, TrackIssuerLogic trackIssuerLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.trackKeyLogic = trackKeyLogic;
            this.trackIssuerLogic = trackIssuerLogic;
        }

        public Saml2Configuration GetSamlUpConfig(SamlUpParty party, bool includeSigningAndDecryptionCertificate = false)
        {
            var samlConfig = new Saml2Configuration();
            if (party != null)
            {
                samlConfig.AllowedIssuer = party.Issuer;
            }

            samlConfig.Issuer = !string.IsNullOrEmpty(party?.SpIssuer) ? party.SpIssuer : trackIssuerLogic.GetIssuer();
            samlConfig.AllowedAudienceUris.Add(samlConfig.Issuer);

            if (party != null)
            {
                samlConfig.SingleSignOnDestination = new Uri(party.AuthnUrl);
                if(!string.IsNullOrEmpty(party?.LogoutUrl))
                {
                    samlConfig.SingleLogoutDestination = new Uri(party.LogoutUrl);
                }

                foreach (var key in party.Keys)
                {
                    samlConfig.SignatureValidationCertificates.Add(key.ToSaml2X509Certificate());
                }
            }

            if (includeSigningAndDecryptionCertificate)
            {
                samlConfig.SigningCertificate = samlConfig.DecryptionCertificate = trackKeyLogic.GetPrimarySaml2X509Certificate(RouteBinding.Key);
            }
            if (party != null)
            {
                samlConfig.SignatureAlgorithm = party.SignatureAlgorithm;
                samlConfig.SignAuthnRequest = party.SignAuthnRequest;

                samlConfig.CertificateValidationMode = party.CertificateValidationMode;
                samlConfig.RevocationMode = party.RevocationMode;
            }

            return samlConfig;
        }

        public Saml2Configuration GetSamlDownConfig(SamlDownParty party, bool includeSigningCertificate = false)
        {
            var samlConfig = new Saml2Configuration();
            samlConfig.Issuer = !string.IsNullOrEmpty(party?.IdPIssuer) ? party.IdPIssuer : trackIssuerLogic.GetIssuer();

            if (party != null)
            {
                if (party.Keys?.Count > 0)
                {
                    foreach (var key in party.Keys)
                    {
                        samlConfig.SignatureValidationCertificates.Add(key.ToSaml2X509Certificate());
                    }
                }
            }

            if (includeSigningCertificate)
            {
                samlConfig.SigningCertificate = trackKeyLogic.GetPrimarySaml2X509Certificate(RouteBinding.Key);
            }
            if (party != null)
            {
                samlConfig.SignatureAlgorithm = party.SignatureAlgorithm;

                samlConfig.CertificateValidationMode = party.CertificateValidationMode;
                samlConfig.RevocationMode = party.RevocationMode;
            }

            return samlConfig;
        }

    }
}
