using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity.Saml2.Schemas;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using FoxIDs.Models.Api;
using ITfoxtec.Identity.Models;

namespace FoxIDs.Client.Models.ViewModels
{
    public class SamlUpPartyViewModel : ISamlClaimTransformViewModel, IUpPartySessionLifetime
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Up-party name")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.SamlParty.IssuerLength)]
        [Display(Name = "Optional custom SP issuer (default auto generated)")]
        public string SpIssuer { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<SamlClaimTransformViewModel> ClaimTransforms { get; set; } = new List<SamlClaimTransformViewModel>();

        [ValidateComplexType]
        [Length(Constants.Models.SamlParty.ClaimsMin, Constants.Models.SamlParty.ClaimsMax, Constants.Models.Claim.SamlTypeLength, Constants.Models.Claim.SamlTypeRegExPattern)]
        [Display(Name = "Claims (in addition to default claims)")]
        public List<string> Claims { get; set; }

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
        public SamlBindingTypes AuthnRequestBinding { get; set; } = SamlBindingTypes.Post;

        [Required]
        [Display(Name = "Authn response binding")]
        public SamlBindingTypes AuthnResponseBinding { get; set; } = SamlBindingTypes.Post;

        [Required]
        [MaxLength(Constants.Models.SamlParty.Up.AuthnUrlLength)]
        [Display(Name = "Authn URL")]
        public string AuthnUrl { get; set; }

        [ValidateComplexType]
        [Length(Constants.Models.SamlParty.Up.KeysMin, Constants.Models.SamlParty.KeysMax)]
        [Display(Name = "One or more signature validation certificates")]
        public List<JsonWebKey> Keys { get; set; }

        [Display(Name = "Logout request binding")]
        public SamlBindingTypes LogoutRequestBinding { get; set; } = SamlBindingTypes.Post;

        [Display(Name = "Logout response binding")]
        public SamlBindingTypes LogoutResponseBinding { get; set; } = SamlBindingTypes.Post;

        [MaxLength(Constants.Models.SamlParty.Up.LogoutUrlLength)]
        [Display(Name = "Logout URL")]
        public string LogoutUrl { get; set; }

        /// <summary>
        /// URL party binding pattern.
        /// </summary>
        [Display(Name = "URL party binding pattern")]
        public PartyBindingPatterns PartyBindingPattern { get; set; } = PartyBindingPatterns.Brackets;

        /// <summary>
        /// Default 10 hours.
        /// </summary>
        [Range(Constants.Models.UpParty.SessionLifetimeMin, Constants.Models.UpParty.SessionLifetimeMax)]
        public int SessionLifetime { get; set; } = 36000;

        /// <summary>
        /// Default 24 hours.
        /// </summary>
        [Range(Constants.Models.UpParty.SessionAbsoluteLifetimeMin, Constants.Models.UpParty.SessionAbsoluteLifetimeMax)]
        public int SessionAbsoluteLifetime { get; set; } = 86400;

        /// <summary>
        /// Default 0 minutes.
        /// </summary>
        [Range(Constants.Models.UpParty.PersistentAbsoluteSessionLifetimeMin, Constants.Models.UpParty.PersistentAbsoluteSessionLifetimeMax)]
        public int PersistentSessionAbsoluteLifetime { get; set; } = 0;

        /// <summary>
        /// Default false.
        /// </summary>
        public bool PersistentSessionLifetimeUnlimited { get; set; } = false;
    }
}
