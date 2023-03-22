using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Models;
using ITfoxtec.Identity.Saml2.Schemas;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;

namespace FoxIDs.Models.Api
{
    public class SamlUpParty : INameValue, IValidatableObject, IClaimTransform<SamlClaimTransform>
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.NoteLength)]
        public string Note { get; set; }

        [Required]
        public PartyUpdateStates UpdateState { get; set; } = PartyUpdateStates.Automatic;

        [Range(Constants.Models.SamlParty.MetadataUpdateRateMin, Constants.Models.SamlParty.MetadataUpdateRateMax)]
        public int? MetadataUpdateRate { get; set; }

        [MaxLength(Constants.Models.SamlParty.MetadataUrlLength)]
        public string MetadataUrl { get; set; }

        /// <summary>
        /// Optional custom SP issuer (default auto generated).
        /// </summary>
        [MaxLength(Constants.Models.SamlParty.IssuerLength)]
        public string SpIssuer { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<SamlClaimTransform> ClaimTransforms { get; set; }

        [Length(Constants.Models.SamlParty.ClaimsMin, Constants.Models.SamlParty.ClaimsMax, Constants.Models.Claim.SamlTypeLength, Constants.Models.Claim.SamlTypeWildcardRegExPattern)]
        public List<string> Claims { get; set; }

        /// <summary>
        /// Default SHA256.
        /// </summary>
        [MaxLength(Constants.Models.SamlParty.SignatureAlgorithmLength)]
        public string SignatureAlgorithm { get; set; } = Saml2SecurityAlgorithms.RsaSha256Signature;

        /// <summary>
        /// Default None.
        /// </summary>
        public X509CertificateValidationMode CertificateValidationMode { get; set; } = X509CertificateValidationMode.None;

        /// <summary>
        /// Default NoCheck.
        /// </summary>
        public X509RevocationMode RevocationMode { get; set; } = X509RevocationMode.NoCheck;

        [MaxLength(Constants.Models.SamlParty.IssuerLength)]
        public string Issuer { get; set; }

        public SamlBindingTypes? AuthnRequestBinding { get; set; }

        [Required]
        public SamlBindingTypes? AuthnResponseBinding { get; set; }

        [MaxLength(Constants.Models.SamlParty.Up.AuthnUrlLength)]
        public string AuthnUrl { get; set; }

        public bool SignAuthnRequest { get; set; }

        [Length(0, Constants.Models.SamlParty.KeysMax)]
        public List<JwtWithCertificateInfo> Keys { get; set; }

        public SamlBindingTypes? LogoutRequestBinding { get; set; }

        public SamlBindingTypes? LogoutResponseBinding { get; set; }

        [MaxLength(Constants.Models.SamlParty.Up.LogoutUrlLength)]
        public string LogoutUrl { get; set; }

        [MaxLength(Constants.Models.SamlParty.Up.LogoutUrlLength)]
        public string SingleLogoutResponseUrl { get; set; }

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

        public bool DisableSingleLogout { get; set; }

        /// <summary>
        /// URL party binding pattern.
        /// </summary>
        public PartyBindingPatterns PartyBindingPattern { get; set; } = PartyBindingPatterns.Brackets;

        public SamlAuthnContextComparisonTypes? AuthnContextComparison { get; set; }

        public List<string> AuthnContextClassReferences { get; set; }

        public bool MetadataAddLogoutResponseLocation { get; set; }

        public bool SignMetadata { get; set; }

        public bool MetadataIncludeEncryptionCertificates { get; set; }

        [Length(Constants.Models.SamlParty.MetadataNameIdFormatsMin, Constants.Models.SamlParty.MetadataNameIdFormatsMax, Constants.Models.Claim.ValueLength, Constants.Models.Claim.SamlTypeRegExPattern)]
        public List<string> MetadataNameIdFormats { get; set; }

        [ValidateComplexType]
        [Length(Constants.Models.SamlParty.MetadataAttributeConsumingServicesMin, Constants.Models.SamlParty.MetadataAttributeConsumingServicesMax)]
        public List<SamlMetadataAttributeConsumingService> MetadataAttributeConsumingServices { get; set; }

        [ValidateComplexType]
        [Length(Constants.Models.SamlParty.MetadataContactPersonsMin, Constants.Models.SamlParty.MetadataContactPersonsMax)]
        public List<SamlMetadataContactPerson> MetadataContactPersons { get; set; }

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
            if (Claims?.Where(c => c == "*").Count() > 1)
            {
                results.Add(new ValidationResult($"Only one wildcard (*) is allowed in the field {nameof(Claims)}.", new[] { nameof(Claims) }));
            }

            if (AuthnResponseBinding == null)
            {
                results.Add(new ValidationResult($"The {nameof(AuthnResponseBinding)} field is required.", new[] { nameof(AuthnResponseBinding) }));
            }

            if (UpdateState == PartyUpdateStates.Manual)
            {
                if (Issuer.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The {nameof(Issuer)} field is required.", new[] { nameof(Issuer) }));
                }
                if (AuthnUrl.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The {nameof(AuthnUrl)} field is required.", new[] { nameof(AuthnUrl) }));
                }
                if (AuthnRequestBinding == null)
                {
                    results.Add(new ValidationResult($"The {nameof(AuthnRequestBinding)} field is required.", new[] { nameof(AuthnRequestBinding) }));
                }
                if (!LogoutUrl.IsNullOrWhiteSpace())
                {
                    if (LogoutRequestBinding == null)
                    {
                        results.Add(new ValidationResult($"The {nameof(LogoutRequestBinding)} field is required.", new[] { nameof(LogoutRequestBinding) }));
                    }
                    if (LogoutResponseBinding == null)
                    {
                        results.Add(new ValidationResult($"The {nameof(LogoutResponseBinding)} field is required.", new[] { nameof(LogoutResponseBinding) }));
                    }
                }
                if (Keys?.Count < Constants.Models.SamlParty.Up.KeysMin)
                {
                    results.Add(new ValidationResult($"The field {nameof(Keys)} must be at least {Constants.Models.SamlParty.Up.KeysMin}.", new[] { nameof(Keys) }));
                }
            }
            else
            {
                if (!MetadataUpdateRate.HasValue)
                {
                    results.Add(new ValidationResult($"The {nameof(MetadataUpdateRate)} field is required.", new[] { nameof(MetadataUpdateRate) }));
                }
                if (MetadataUrl.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The {nameof(MetadataUrl)} field is required.", new[] { nameof(MetadataUrl) }));
                } 
            }
            return results;
        }
    }
}
