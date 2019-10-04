using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity.Saml2.Schemas;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;

namespace FoxIDs.Models.Api
{
    public class SamlDownParty : INameValue
    {
        [MaxLength(Constants.Models.PartyNameLength)]
        [RegularExpression(Constants.Models.PartyNameRegExPattern)]
        public string Name { get; set; }

        [Length(Constants.Models.DownParty.AllowUpPartyNamesMin, Constants.Models.DownParty.AllowUpPartyNamesMax, Constants.Models.PartyNameLength, Constants.Models.PartyNameRegExPattern)]
        public List<string> AllowUpPartyNames { get; set; }

        [MaxLength(Constants.Models.SamlParty.IssuerLength)]
        public string IdSIssuer { get; set; }

        [MaxLength(Constants.Models.SamlParty.IssuerLength)]

        [Length(Constants.Models.SamlParty.Down.ClaimsMin, Constants.Models.SamlParty.Down.ClaimsMax, Constants.Models.SamlParty.Down.ClaimsLength)]
        public List<string> Claims { get; set; }

        /// <summary>
        /// Default 20 days.
        /// </summary>
        [Range(Constants.Models.SamlParty.MetadataLifetimeMin, Constants.Models.SamlParty.MetadataLifetimeMax)]
        public int MetadataLifetime { get; set; } = 1728000;

        [Range(Constants.Models.SamlParty.Down.SubjectConfirmationLifetimeMin, Constants.Models.SamlParty.Down.SubjectConfirmationLifetimeMax)]
        public int SubjectConfirmationLifetime { get; set; }

        [Range(Constants.Models.SamlParty.Down.IssuedTokenLifetimeMin, Constants.Models.SamlParty.Down.IssuedTokenLifetimeMax)]
        public int IssuedTokenLifetime { get; set; }

        /// <summary>
        /// Default SHA256.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.SamlParty.SignatureAlgorithmLength)]
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
        [MaxLength(Constants.Models.SamlParty.IssuerLength)]
        public string Issuer { get; set; }

        [Required]
        public SamlBinding AuthnBinding { get; set; }

        [Length(Constants.Models.SamlParty.Down.AcsUrlsMin, Constants.Models.SamlParty.Down.AcsUrlsMax, Constants.Models.SamlParty.Down.AcsUrlsLength)]
        public List<string> AcsUrls { get; set; }

        [ValidateObject]
        public SamlBinding LogoutBinding { get; set; }

        [MaxLength(Constants.Models.SamlParty.Down.SingleLogoutUrlLength)]
        public string SingleLogoutUrl { get; set; }

        [MaxLength(Constants.Models.SamlParty.Down.LoggedOutUrlLength)]
        public string LoggedOutUrl { get; set; }

        [Length(Constants.Models.SamlParty.KeysMin, Constants.Models.SamlParty.KeysMax)]
        public List<JsonWebKey> Keys { get; set; }
    }
}
