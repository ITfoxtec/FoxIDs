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
    public class SamlDownPartyViewModel : IAllowUpPartyNames, IDownPartyName, IValidatableObject
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Down-party name")]
        public string Name { get; set; }

        [ValidateComplexType]
        [Length(Constants.Models.DownParty.AllowUpPartyNamesMin, Constants.Models.DownParty.AllowUpPartyNamesMax, Constants.Models.Party.NameLength, Constants.Models.Party.NameRegExPattern)]
        [Display(Name = "Allow up-party names")]
        public List<string> AllowUpPartyNames { get; set; } = new List<string>();

        [MaxLength(Constants.Models.SamlParty.IssuerLength)]
        [Display(Name = "Optional custom issuer (default auto generated)")]
        public string IdSIssuer { get; set; }

        [ValidateComplexType]
        [Length(Constants.Models.SamlParty.ClaimsMin, Constants.Models.SamlParty.ClaimsMax, Constants.Models.SamlParty.ClaimLength)]
        [Display(Name = "Issue of claim (in addition to default claims)")]
        public List<string> Claims { get; set; }

        /// <summary>
        /// Default 20 days.
        /// </summary>
        [Range(Constants.Models.SamlParty.MetadataLifetimeMin, Constants.Models.SamlParty.MetadataLifetimeMax)]
        [Display(Name = "Metadata lifetime in seconds")]
        public int MetadataLifetime { get; set; } = 1728000;

        /// <summary>
        /// Default 5 minutes.
        /// </summary>
        [Range(Constants.Models.SamlParty.Down.SubjectConfirmationLifetimeMin, Constants.Models.SamlParty.Down.SubjectConfirmationLifetimeMax)]
        [Display(Name = "Subject confirmation lifetime in seconds")]
        public int SubjectConfirmationLifetime { get; set; } = 300;

        /// <summary>
        /// Default 60 minutes.
        /// </summary>
        [Range(Constants.Models.SamlParty.Down.IssuedTokenLifetimeMin, Constants.Models.SamlParty.Down.IssuedTokenLifetimeMax)]
        [Display(Name = "Issued token lifetime in seconds")]
        public int IssuedTokenLifetime { get; set; } = 3600;

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

        [ValidateComplexType]
        [Length(Constants.Models.SamlParty.Down.AcsUrlsMin, Constants.Models.SamlParty.Down.AcsUrlsMax, Constants.Models.SamlParty.Down.AcsUrlsLength)]
        [Display(Name = "Assertion consumer service (ACS) url")]
        public List<string> AcsUrls { get; set; } 

        [Display(Name = "Logout request binding")]
        public SamlBindingTypes LogoutRequestBinding { get; set; } = SamlBindingTypes.Post;

        [Display(Name = "Logout response binding")]
        public SamlBindingTypes LogoutResponseBinding { get; set; } = SamlBindingTypes.Post;

        [MaxLength(Constants.Models.SamlParty.Down.SingleLogoutUrlLength)]
        [Display(Name = "Optional single logout url")]
        public string SingleLogoutUrl { get; set; }

        [MaxLength(Constants.Models.SamlParty.Down.LoggedOutUrlLength)]
        [Display(Name = "Optional logged out url")]
        public string LoggedOutUrl { get; set; }

        [ValidateComplexType]
        [Length(Constants.Models.SamlParty.Down.KeysMin, Constants.Models.SamlParty.KeysMax)]
        [Display(Name = "Optional one or more signature validation certificates")]
        public List<JsonWebKey> Keys { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (AllowUpPartyNames?.Count <= 0)
            {
                results.Add(new ValidationResult($"At least one in the field {nameof(AllowUpPartyNames)} is required.", new[] { nameof(AllowUpPartyNames) }));
            }
            return results;
        }
    }
}
