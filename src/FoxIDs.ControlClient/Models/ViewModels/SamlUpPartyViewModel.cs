using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity.Saml2.Schemas;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class SamlUpPartyViewModel 
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Up Party name")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.SamlParty.IssuerLength)]
        [Display(Name = "Optional custom issuer (default auto generated)")]
        public string IdSIssuer { get; set; }

        /// <summary>
        /// Default 20 days.
        /// </summary>
        [Range(Constants.Models.SamlParty.MetadataLifetimeMin, Constants.Models.SamlParty.MetadataLifetimeMax)]
        [Display(Name = "Metadata lifetime in seconds")]
        public int MetadataLifetime { get; set; } = 1728000;

        /// <summary>
        /// Default SHA256.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.SamlParty.SignatureAlgorithmLength)]
        [Display(Name = "Signature algorithm")]
        public string SignatureAlgorithm { get; set; } = Saml2SecurityAlgorithms.RsaSha256Signature;

        /// <summary>
        /// Default None.
        /// </summary>
        [Required]
        [Display(Name = "Certificate validation mode")]
        public X509CertificateValidationMode CertificateValidationMode { get; set; } = X509CertificateValidationMode.None;

        /// <summary>
        /// Default NoCheck.
        /// </summary>
        [Required]
        [Display(Name = "Revocation mode")]
        public X509RevocationMode RevocationMode { get; set; } = X509RevocationMode.NoCheck;

        [Required]
        [MaxLength(Constants.Models.SamlParty.IssuerLength)]
        [Display(Name = "Issuer")]
        public string Issuer { get; set; }

        [Required]
        [Display(Name = "Authn request binding")]
        public SamlBindingType AuthnRequestBinding { get; set; } = SamlBindingType.Redirect;

        [Required]
        [Display(Name = "Authn response binding")]
        public SamlBindingType AuthnResponseBinding { get; set; } = SamlBindingType.Post;

        [Required]
        [MaxLength(Constants.Models.SamlParty.Up.AuthnUrlLength)]
        [Display(Name = "Authn url")]
        public string AuthnUrl { get; set; }

        [Required]
        [Length(Constants.Models.SamlParty.Up.KeysMin, Constants.Models.SamlParty.KeysMax)]
        [Display(Name = "One or more signature validation certificates")]
        public List<JsonWebKey> Keys { get; set; }

        [Display(Name = "Logout request binding")]
        public SamlBindingType LogoutRequestBinding { get; set; } = SamlBindingType.Post;

        [Display(Name = "Logout response binding")]
        public SamlBindingType LogoutResponseBinding { get; set; } = SamlBindingType.Post;

        [MaxLength(Constants.Models.SamlParty.Up.LogoutUrlLength)]
        [Display(Name = "Logout url")]
        public string LogoutUrl { get; set; }
    }
}
