using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity.Saml2.Schemas;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using Api = FoxIDs.Models.Api;

namespace FoxIDs.Models.Api
{
    public class SamlUpParty : INameValue
    {
        [MaxLength(Constants.Models.PartyNameLength)]
        [RegularExpression(Constants.Models.PartyNameRegExPattern)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.SamlUpParty.IssuerLength)]
        public string IdSIssuer { get; set; }

        /// <summary>
        /// Default 20 days.
        /// </summary>
        [Range(Constants.Models.SamlUpParty.MetadataLifetimeMin, Constants.Models.SamlUpParty.MetadataLifetimeMax)]
        public int MetadataLifetime { get; set; } = 1728000;

        /// <summary>
        /// Default SHA256.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.SamlUpParty.SignatureAlgorithmLength)]
        public string SignatureAlgorithm { get; set; } = Saml2SecurityAlgorithms.RsaSha256Signature;

        /// <summary>
        /// Default None.
        /// </summary>
        [Required]
        public X509CertificateValidationMode CertificateValidationMode { get; set; } = X509CertificateValidationMode.None;

        /// <summary>
        /// Default NoCheck.
        /// </summary>
        [Required]
        public X509RevocationMode RevocationMode { get; set; } = X509RevocationMode.NoCheck;

        [Required]
        [MaxLength(Constants.Models.SamlUpParty.IssuerLength)]
        public string Issuer { get; set; }

        [Required]
        public SamlBinding AuthnBinding { get; set; }

        [Required]
        [MaxLength(Constants.Models.SamlUpParty.AuthnUrlLength)]
        public string AuthnUrl { get; set; }

        [Required]
        [Length(Constants.Models.SamlUpParty.KeysMin, Constants.Models.SamlUpParty.KeysMax)]
        public List<Api.JsonWebKey> Keys { get; set; }

        [ValidateObject]
        public SamlBinding LogoutBinding { get; set; }

        [MaxLength(Constants.Models.SamlUpParty.LogoutUrlLength)]
        public string LogoutUrl { get; set; }
    }
}
