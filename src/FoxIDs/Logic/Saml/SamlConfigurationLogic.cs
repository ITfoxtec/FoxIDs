using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using ITfoxtec.Identity.Saml2;

namespace FoxIDs.Logic
{
    public class Saml2ConfigurationLogic : LogicSequenceBase
    {
        private readonly TrackKeyLogic trackKeyLogic;
        private readonly TrackIssuerLogic trackIssuerLogic;

        public Saml2ConfigurationLogic(TrackKeyLogic trackKeyLogic, TrackIssuerLogic trackIssuerLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.trackKeyLogic = trackKeyLogic;
            this.trackIssuerLogic = trackIssuerLogic;
        }

        public async Task<FoxIDsSaml2Configuration> GetSamlUpConfigAsync(SamlUpParty party, bool includeSigningAndDecryptionCertificate = false, bool includeSignatureValidationCertificates = true)
        {
            var samlConfig = new FoxIDsSaml2Configuration();
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

                if (includeSignatureValidationCertificates)
                {
                    var partyCertificates = party.Keys.ToSaml2X509Certificates();
                    foreach (var partyCertificate in partyCertificates)
                    {
                        if (partyCertificate.IsValidLocalTime())
                        {
                            samlConfig.SignatureValidationCertificates.Add(partyCertificate);
                        }
                        else
                        {
                            samlConfig.InvalidSignatureValidationCertificates.Add(partyCertificate);
                        }
                    }
                }
            }

            if (includeSigningAndDecryptionCertificate)
            {
                samlConfig.SigningCertificate = samlConfig.DecryptionCertificate = await trackKeyLogic.GetPrimarySaml2X509CertificateAsync(RouteBinding.Key);
                samlConfig.SecondaryDecryptionCertificate = trackKeyLogic.GetSecondarySaml2X509Certificate(RouteBinding.Key);
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

        public async Task<FoxIDsSaml2Configuration> GetSamlDownConfigAsync(SamlDownParty party, bool includeSigningCertificate = false, bool includeSignatureValidationCertificates = true, bool includeEncryptionCertificates = false)
        {
            var samlConfig = new FoxIDsSaml2Configuration();
            samlConfig.Issuer = !string.IsNullOrEmpty(party?.IdPIssuer) ? party.IdPIssuer : trackIssuerLogic.GetIssuer();

            if (party != null)
            {
                if (includeSignatureValidationCertificates && party.Keys?.Count > 0)
                {
                    var partyCertificates = party.Keys.ToSaml2X509Certificates();
                    foreach (var partyCertificate in partyCertificates)
                    {
                        if (partyCertificate.IsValidLocalTime())
                        {
                            samlConfig.SignatureValidationCertificates.Add(partyCertificate);
                        }
                        else
                        {
                            samlConfig.InvalidSignatureValidationCertificates.Add(partyCertificate);
                        }
                    }
                }

                if (includeEncryptionCertificates && party.EncryptAuthnResponse && party.EncryptionKey != null)
                {
                    samlConfig.EncryptionCertificate = party.EncryptionKey.ToSaml2X509Certificate();
                }
            }

            if (includeSigningCertificate)
            {
                samlConfig.SigningCertificate = await trackKeyLogic.GetPrimarySaml2X509CertificateAsync(RouteBinding.Key);
            }
            if (party != null)
            {
                samlConfig.SignatureAlgorithm = party.SignatureAlgorithm;

                samlConfig.CertificateValidationMode = party.CertificateValidationMode;
                samlConfig.RevocationMode = party.RevocationMode;
            }

            return samlConfig;
        }      

        public Exception GetInvalidSignatureValidationCertificateException(FoxIDsSaml2Configuration samlConfig, Exception ex)
        {
            if (samlConfig.InvalidSignatureValidationCertificates?.Count() > 0)
            {
                var certInfo = samlConfig.InvalidSignatureValidationCertificates.Select(c => $"'{c.Subject}, Valid from {c.NotBefore.ToShortDateString()} to {c.NotAfter.ToShortDateString()}, Thumbprint: {c.Thumbprint}'");
                return new Exception($"Invalid signature validation certificates {string.Join(", ", certInfo)}.", ex);
            }
            return null;
        }
    }
}
