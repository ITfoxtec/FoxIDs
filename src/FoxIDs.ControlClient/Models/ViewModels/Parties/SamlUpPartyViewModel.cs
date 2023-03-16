using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity.Saml2.Schemas;
using System.ServiceModel.Security;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;

namespace FoxIDs.Client.Models.ViewModels
{
    public class SamlUpPartyViewModel : IValidatableObject, ISamlClaimTransformViewModel, IUpPartySessionLifetime, IUpPartyHrd, ISamlMetadataAttributeConsumingServiceVievModel, ISamlMetadataContactPersonVievModel
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Up-party name")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.NoteLength)]
        [Display(Name = "Your notes")]
        public string Note { get; set; }

        public bool IsManual { get; set; }

        public bool AutomaticStopped { get; set; }

        [Range(Constants.Models.SamlParty.MetadataUpdateRateMin, Constants.Models.SamlParty.MetadataUpdateRateMax)]
        [Display(Name = "Automatic update rate in seconds")]
        public int MetadataUpdateRate { get; set; } = 172800; // 2 days

        [MaxLength(Constants.Models.SamlParty.MetadataUrlLength)]
        [Display(Name = "Metadata URL")]
        public string MetadataUrl { get; set; }

        [Display(Name = "Automatic update")]
        public bool AutomaticUpdate 
        {
            get 
            {
                AutomaticStopped = false;
                return !IsManual; 
            }
            set
            {
                AutomaticStopped = false;
                IsManual = !value;
            }
        }

        [MaxLength(Constants.Models.SamlParty.IssuerLength)]
        [Display(Name = "Optional custom SP issuer (default auto generated)")]
        public string SpIssuer { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<SamlClaimTransformViewModel> ClaimTransforms { get; set; } = new List<SamlClaimTransformViewModel>();

        [ValidateComplexType]
        [Length(Constants.Models.SamlParty.ClaimsMin, Constants.Models.SamlParty.ClaimsMax, Constants.Models.Claim.SamlTypeLength, Constants.Models.Claim.SamlTypeWildcardRegExPattern)]
        [Display(Name = "Forward claims (use * to carried all claims forward)")]
        public List<string> Claims { get; set; }

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

        [MaxLength(Constants.Models.SamlParty.IssuerLength)]
        [Display(Name = "Issuer")]
        public string Issuer { get; set; }

        [Display(Name = "Authn request binding")]
        public SamlBindingTypes AuthnRequestBinding { get; set; } = SamlBindingTypes.Post;

        [Display(Name = "Authn response binding")]
        public SamlBindingTypes AuthnResponseBinding { get; set; } = SamlBindingTypes.Post;

        [MaxLength(Constants.Models.SamlParty.Up.AuthnUrlLength)]
        [Display(Name = "Authn URL")]
        public string AuthnUrl { get; set; }

        [Display(Name = "Sign authn request")]
        public bool SignAuthnRequest { get; set; }

        [ValidateComplexType]
        [Length(0, Constants.Models.SamlParty.KeysMax)]
        [Display(Name = "One or more signature validation certificates")]
        public List<JwtWithCertificateInfo> Keys { get; set; }

        [Display(Name = "Logout request binding")]
        public SamlBindingTypes LogoutRequestBinding { get; set; } = SamlBindingTypes.Post;

        [Display(Name = "Logout response binding")]
        public SamlBindingTypes LogoutResponseBinding { get; set; } = SamlBindingTypes.Post;

        [MaxLength(Constants.Models.SamlParty.Up.LogoutUrlLength)]
        [Display(Name = "Logout URL")]
        public string LogoutUrl { get; set; }

        [MaxLength(Constants.Models.SamlParty.Up.LogoutUrlLength)]
        [Display(Name = "Single logout response URL (optional, default logout URL is used)")]
        public string SingleLogoutResponseUrl { get; set; }

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

        [Display(Name = "Single logout")]
        public bool EnableSingleLogout { get; set; } = true;

        [Display(Name = "Optional Authn context comparison")]
        public SamlAuthnContextComparisonTypesVievModel AuthnContextComparisonViewModel { get; set; }

        [Display(Name = "Optional Authn context class references")]
        public List<string> AuthnContextClassReferences { get; set; } = new List<string>();

        [Display(Name = "Add logout response location URL in metadata")]
        public bool MetadataAddLogoutResponseLocation { get; set; }

        [Display(Name = "Sign metadata")]
        public bool SignMetadata { get; set; }

        [Display(Name = "Include encryption certificates in metadata")]
        public bool MetadataIncludeEncryptionCertificates { get; set; }

        [Length(Constants.Models.SamlParty.MetadataNameIdFormatsMin, Constants.Models.SamlParty.MetadataNameIdFormatsMax, Constants.Models.Claim.ValueLength, Constants.Models.Claim.SamlTypeRegExPattern)]
        [Display(Name = "Optional NameID formats in metadata")]
        public List<string> MetadataNameIdFormats { get; set; }

        [ValidateComplexType]
        [Length(Constants.Models.SamlParty.MetadataAttributeConsumingServicesMin, Constants.Models.SamlParty.MetadataAttributeConsumingServicesMax)]
        public List<SamlMetadataAttributeConsumingService> MetadataAttributeConsumingServices { get; set; } = new List<SamlMetadataAttributeConsumingService>();

        [ValidateComplexType]
        [Length(Constants.Models.SamlParty.MetadataContactPersonsMin, Constants.Models.SamlParty.MetadataContactPersonsMax)]
        public List<SamlMetadataContactPerson> MetadataContactPersons { get; set; } = new List<SamlMetadataContactPerson>();

        /// <summary>
        /// Home realm discovery (HRD) domains.
        /// </summary>
        [Length(Constants.Models.UpParty.HrdDomainMin, Constants.Models.UpParty.HrdDomainMax, Constants.Models.UpParty.HrdDomainLength, Constants.Models.UpParty.HrdDomainRegExPattern)]
        [Display(Name = "HRD domains")]
        public List<string> HrdDomains { get; set; }

        [Display(Name = "Show HRD button with domain")]
        public bool HrdShowButtonWithDomain { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) display name.
        /// </summary>
        [MaxLength(Constants.Models.UpParty.HrdDisplayNameLength)]
        [RegularExpression(Constants.Models.UpParty.HrdDisplayNameRegExPattern)]
        [Display(Name = "HRD display name")]
        public string HrdDisplayName { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) logo URL.
        /// </summary>
        [MaxLength(Constants.Models.UpParty.HrdLogoUrlLength)]
        [RegularExpression(Constants.Models.UpParty.HrdLogoUrlRegExPattern)]
        [Display(Name = "HRD logo URL")]
        public string HrdLogoUrl { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (IsManual)
            {
                if (Issuer.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The {nameof(Issuer)} field is required.", new[] { nameof(Issuer) }));
                }
                if (AuthnUrl.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The {nameof(AuthnUrl)} field is required.", new[] { nameof(AuthnUrl) }));
                }
                if (Keys?.Count < Constants.Models.SamlParty.Up.KeysMin)
                {
                    results.Add(new ValidationResult($"The field {nameof(Keys)} must be at least {Constants.Models.SamlParty.Up.KeysMin}.", new[] { nameof(Keys) }));
                }
            }
            else
            {
                if (MetadataUrl.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The {nameof(MetadataUrl)} field is required.", new[] { nameof(MetadataUrl) }));
                }
            }
            return results;
        }
    }
}
