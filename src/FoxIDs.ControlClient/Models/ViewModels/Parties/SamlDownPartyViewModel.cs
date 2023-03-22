using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity.Saml2.Schemas;
using System.ServiceModel.Security;
using FoxIDs.Models.Api;
using Newtonsoft.Json;

namespace FoxIDs.Client.Models.ViewModels
{
    public class SamlDownPartyViewModel : IValidatableObject, IAllowUpPartyNames, IDownPartyName, ISamlClaimTransformViewModel, ISamlMetadataContactPersonVievModel
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Down-party name")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.NoteLength)]
        [Display(Name = "Your notes")]
        public string Note { get; set; }

        [ValidateComplexType]
        [Length(Constants.Models.DownParty.AllowUpPartyNamesMin, Constants.Models.DownParty.AllowUpPartyNamesMax, Constants.Models.Party.NameLength, Constants.Models.Party.NameRegExPattern)]
        [Display(Name = "Allow up-party names")]
        public List<string> AllowUpPartyNames { get; set; } = new List<string>();

        [MaxLength(Constants.Models.SamlParty.IssuerLength)]
        [Display(Name = "Optional custom IdP issuer (default auto generated)")]
        public string IdPIssuer { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<SamlClaimTransformViewModel> ClaimTransforms { get; set; } = new List<SamlClaimTransformViewModel>();

        [ValidateComplexType]
        [Length(Constants.Models.SamlParty.ClaimsMin, Constants.Models.SamlParty.ClaimsMax, Constants.Models.Claim.SamlTypeLength, Constants.Models.Claim.SamlTypeWildcardRegExPattern)]
        [Display(Name = "Issue claims (use * to issue all claims)")]
        public List<string> Claims { get; set; }

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
        /// URL party binding pattern.
        /// </summary>
        [Display(Name = "URL party binding pattern")]
        public PartyBindingPatterns PartyBindingPattern { get; set; } = PartyBindingPatterns.Brackets;

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
        [Display(Name = "Assertion consumer service (ACS) URL")]
        public List<string> AcsUrls { get; set; }

        [Display(Name = "Encrypt authn response")]
        public bool EncryptAuthnResponse { get; set; }

        [Display(Name = "Optional NameId format (otherwise set dynamically)")]
        public string NameIdFormat { get; set; }

        [Display(Name = "Logout request binding")]
        public SamlBindingTypes LogoutRequestBinding { get; set; } = SamlBindingTypes.Post;

        [Display(Name = "Logout response binding")]
        public SamlBindingTypes LogoutResponseBinding { get; set; } = SamlBindingTypes.Post;

        [MaxLength(Constants.Models.SamlParty.Down.SingleLogoutUrlLength)]
        [Display(Name = "Optional single logout URL")]
        public string SingleLogoutUrl { get; set; }

        [MaxLength(Constants.Models.SamlParty.Down.LoggedOutUrlLength)]
        [Display(Name = "Optional logged out URL")]
        public string LoggedOutUrl { get; set; }

        [ValidateComplexType]
        [Length(Constants.Models.SamlParty.Down.KeysMin, Constants.Models.SamlParty.KeysMax)]
        [Display(Name = "Optional one or more signature validation certificates")]
        public List<JwtWithCertificateInfo> Keys { get; set; }

        [Display(Name = "Optional encryption certificate")]
        public JwtWithCertificateInfo EncryptionKey { get; set; }
        
        [Display(Name = "Add logout response location URL in metadata")]
        public bool MetadataAddLogoutResponseLocation { get; set; }

        [Display(Name = "Sign metadata")]
        public bool SignMetadata { get; set; }

        [Display(Name = "Include encryption certificates in metadata")]
        public bool MetadataIncludeEncryptionCertificates { get; set; }

        [Length(Constants.Models.SamlParty.MetadataNameIdFormatsMin, Constants.Models.SamlParty.MetadataNameIdFormatsMax, Constants.Models.Claim.ValueLength, Constants.Models.Claim.SamlTypeRegExPattern)]
        [Display(Name = "Optional NameID formats in metadata")]
        public List<string> MetadataNameIdFormats { get; set; }

        [Length(Constants.Models.SamlParty.MetadataContactPersonsMin, Constants.Models.SamlParty.MetadataContactPersonsMax)]
        public List<SamlMetadataContactPerson> MetadataContactPersons { get; set; } = new List<SamlMetadataContactPerson>();

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
