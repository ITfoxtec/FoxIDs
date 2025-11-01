using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Schemas;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;

namespace FoxIDs.Models.Api
{
    public class SamlUpParty : INameValue, INewNameValue, IClaimTransformRef<SamlClaimTransform>, IExtendedUisRef, ILinkExternalUserRef, IExitClaimTransformsRef<OAuthClaimTransform>, IValidatableObject
    {
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string NewName { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        public string DisplayName { get; set; }

        [MaxLength(Constants.Models.Party.NoteLength)]
        public string Note { get; set; }

        [Required]
        public PartyUpdateStates UpdateState { get; set; } = PartyUpdateStates.Automatic;

        [Range(Constants.Models.SamlParty.MetadataUpdateRateMin, Constants.Models.SamlParty.MetadataUpdateRateMax)]
        public int? MetadataUpdateRate { get; set; } = 172800; // 2 days

        [MaxLength(Constants.Models.SamlParty.MetadataUrlLength)]
        public string MetadataUrl { get; set; }


        /// <summary>
        /// Claim transforms.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<SamlClaimTransform> ClaimTransforms { get; set; }

        /// <summary>
        /// Extended UIs.
        /// </summary>
        [ListLength(Constants.Models.ExtendedUi.UisMin, Constants.Models.ExtendedUi.UisMax)]
        public List<ExtendedUi> ExtendedUis { get; set; }

        /// <summary>
        /// Claim transforms executed before exit / response from authentication method and after the external users claims has been loaded.
        /// </summary>
        [Obsolete("Delete after 2026-07-01.")]
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransform> ExternalUserLoadedClaimTransforms
        {
            get
            {
                return ExitClaimTransforms;
            }
            set
            {
                if (value?.Count > 0)
                {
                    ExitClaimTransforms = ExternalUserLoadedClaimTransforms;
                }
            }
        }

        /// <summary>
        /// Claim transforms executed before exit / response from authentication method and after the external users claims has been loaded.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransform> ExitClaimTransforms { get; set; }

        [ListLength(Constants.Models.SamlParty.ClaimsMin, Constants.Models.SamlParty.ClaimsMax, Constants.Models.Claim.SamlTypeLength, Constants.Models.Claim.SamlTypeWildcardRegExPattern)]
        public List<string> Claims { get; set; }

        /// <summary>
        /// Default.
        /// </summary>
        [MaxLength(Constants.Models.SamlParty.SignatureAlgorithmLength)]
        [Display(Name = "XML canonicalization method")]
        public string XmlCanonicalizationMethod { get; set; } = Constants.Saml.XmlCanonicalizationMethod.XmlDsigExcC14NTransformUrl;

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

        [MaxLength(Constants.Models.Party.IssuerLength)]
        public string Issuer { get; set; }

        /// <summary>
        /// Optional custom SP issuer / audience (default auto generated).
        /// </summary>
        [MaxLength(Constants.Models.Party.IssuerLength)]
        public string SpIssuer { get; set; }

        public SamlBindingTypes? AuthnRequestBinding { get; set; }

        [Required]
        public SamlBindingTypes? AuthnResponseBinding { get; set; }

        [MaxLength(Constants.Models.SamlParty.Up.AuthnUrlLength)]
        public string AuthnUrl { get; set; }

        public bool SignAuthnRequest { get; set; }

        [ListLength(0, Constants.Models.SamlParty.KeysMax)]
        public List<JwkWithCertificateInfo> Keys { get; set; }

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
        /// URL binding pattern.
        /// </summary>
        public PartyBindingPatterns PartyBindingPattern { get; set; } = PartyBindingPatterns.Brackets;

        public SamlAuthnContextComparisonTypes? AuthnContextComparison { get; set; }

        public List<string> AuthnContextClassReferences { get; set; }

        public string AuthnRequestExtensionsXml { get; set; }

        public bool DisableLoginHint { get; set; }

        public bool MetadataAddLogoutResponseLocation { get; set; }

        public bool SignMetadata { get; set; }

        public bool MetadataIncludeEncryptionCertificates { get; set; }

        [ListLength(Constants.Models.SamlParty.MetadataNameIdFormatsMin, Constants.Models.SamlParty.MetadataNameIdFormatsMax, Constants.Models.Claim.LimitedValueLength, Constants.Models.Claim.SamlTypeRegExPattern)]
        public List<string> MetadataNameIdFormats { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.SamlParty.MetadataAttributeConsumingServicesMin, Constants.Models.SamlParty.MetadataAttributeConsumingServicesMax)]
        public List<SamlMetadataAttributeConsumingService> MetadataAttributeConsumingServices { get; set; }

        [ValidateComplexType]
        public SamlMetadataOrganization MetadataOrganization { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.SamlParty.MetadataContactPersonsMin, Constants.Models.SamlParty.MetadataContactPersonsMax)]
        public List<SamlMetadataContactPerson> MetadataContactPersons { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) IP addresses and IP ranges.
        /// </summary>
        [ListLength(Constants.Models.UpParty.HrdIPAddressAndRangeMin, Constants.Models.UpParty.HrdIPAddressAndRangeMax, Constants.Models.UpParty.HrdIPAddressAndRangeLength, Constants.Models.UpParty.HrdIPAddressAndRangeRegExPattern, Constants.Models.UpParty.HrdIPAddressAndRangeTotalMax)]
        [Display(Name = "HRD IP addresses and IP ranges")]
        public List<string> HrdIPAddressesAndRanges { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) domains.
        /// </summary>
        [ListLength(Constants.Models.UpParty.HrdDomainMin, Constants.Models.UpParty.HrdDomainMax, Constants.Models.UpParty.HrdDomainLength, Constants.Models.UpParty.HrdDomainRegExPattern, Constants.Models.UpParty.HrdDomainTotalMax)]
        [Display(Name = "HRD domains")]
        public List<string> HrdDomains { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) regular expressions.
        /// </summary>
        [ListLength(Constants.Models.UpParty.HrdRegularExpressionMin, Constants.Models.UpParty.HrdRegularExpressionMax, Constants.Models.UpParty.HrdRegularExpressionLength, Constants.Models.UpParty.HrdRegularExpressionTotalMax)]
        [Display(Name = "HRD regular expressions")]
        public List<string> HrdRegularExpressions { get; set; }

        [Display(Name = "Show HRD button while using IP address / range, HRD domain or regular expression")]
        public bool HrdAlwaysShowButton { get; set; }

        [Display(Name = "Show HRD button while using HRD domain")]
        [Obsolete($"Use {nameof(HrdAlwaysShowButton)} instead.")]
        public bool? HrdShowButtonWithDomain { get; set; }

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

        [Display(Name = "Disable user authentication trust")]
        public bool DisableUserAuthenticationTrust { get; set; }

        [Display(Name = "Disable token exchange trust")]
        public bool DisableTokenExchangeTrust { get; set; }

        [Display(Name = "Enable IdP-Initiated login")]
        public bool EnableIdPInitiated { get; set; }

        [Range(Constants.Models.SamlParty.Up.IdPInitiatedGrantLifetimeMin, Constants.Models.SamlParty.Up.IdPInitiatedGrantLifetimeMax)]
        [Display(Name = "IdP-Initiated grant lifetime for OpenID Connect applications (active if greater than 0)")]
        public int? IdPInitiatedGrantLifetime { get; set; }

        [ValidateComplexType]
        public LinkExternalUser LinkExternalUser { get; set; }

        [ListLength(Constants.Models.UpParty.ProfilesMin, Constants.Models.UpParty.ProfilesMax)]
        [Display(Name = "Profiles")]
        public List<SamlUpPartyProfile> Profiles { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Name.IsNullOrWhiteSpace() && DisplayName.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"Require either a Name or Display Name.", [nameof(Name), nameof(DisplayName)]));
            }
            if (DisableUserAuthenticationTrust && DisableTokenExchangeTrust)
            {
                results.Add(new ValidationResult($"Both the {nameof(DisableUserAuthenticationTrust)} and the {nameof(DisableTokenExchangeTrust)} can not be disabled at the same time.", [nameof(DisableUserAuthenticationTrust), nameof(DisableTokenExchangeTrust)]));
            }

            if (Claims?.Where(c => c == "*").Count() > 1)
            {
                results.Add(new ValidationResult($"Only one wildcard (*) is allowed in the field {nameof(Claims)}.", [nameof(Claims)]));
            }

            if (AuthnResponseBinding == null)
            {
                results.Add(new ValidationResult($"The {nameof(AuthnResponseBinding)} field is required.", [nameof(AuthnResponseBinding)]));
            }

            if (UpdateState == PartyUpdateStates.Manual)
            {
                if (Issuer.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The {nameof(Issuer)} field is required. If '{nameof(UpdateState)}' is '{PartyUpdateStates.Manual}'.", [nameof(Issuer)]));
                }
                if (AuthnUrl.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The {nameof(AuthnUrl)} field is required. If '{nameof(UpdateState)}' is '{PartyUpdateStates.Manual}'.", [nameof(AuthnUrl)]));
                }
                if (AuthnRequestBinding == null)
                {
                    results.Add(new ValidationResult($"The {nameof(AuthnRequestBinding)} field is required. If '{nameof(UpdateState)}' is '{PartyUpdateStates.Manual}'.", [nameof(AuthnRequestBinding)]));
                }
                if (!LogoutUrl.IsNullOrWhiteSpace())
                {
                    if (LogoutRequestBinding == null)
                    {
                        results.Add(new ValidationResult($"The {nameof(LogoutRequestBinding)} field is required.", [nameof(LogoutRequestBinding)]));
                    }
                    if (LogoutResponseBinding == null)
                    {
                        results.Add(new ValidationResult($"The {nameof(LogoutResponseBinding)} field is required.", [nameof(LogoutResponseBinding)]));
                    }
                }
                if (Keys?.Count < Constants.Models.SamlParty.Up.KeysMin)
                {
                    results.Add(new ValidationResult($"The field {nameof(Keys)} must be at least {Constants.Models.SamlParty.Up.KeysMin}. If '{nameof(UpdateState)}' is '{PartyUpdateStates.Manual}'.", [nameof(Keys)]));
                }
            }
            else
            {
                if (!MetadataUpdateRate.HasValue)
                {
                    results.Add(new ValidationResult($"The {nameof(MetadataUpdateRate)} field is required. If '{nameof(UpdateState)}' is different from '{PartyUpdateStates.Manual}'.", [nameof(MetadataUpdateRate)]));
                }
                if (MetadataUrl.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The {nameof(MetadataUrl)} field is required. If '{nameof(UpdateState)}' is different from '{PartyUpdateStates.Manual}'.", [nameof(MetadataUrl)]));
                } 
            }

            if (Profiles != null)
            {
                var count = 0;
                foreach (var profile in Profiles)
                {
                    count++;
                    if ((Name.Length + profile.Name?.Length) > Constants.Models.Party.NameLength)
                    {
                        results.Add(new ValidationResult($"The fields {nameof(Name)} (value: '{Name}') and {nameof(profile.Name)} (value: '{profile.Name}') must not be more then {Constants.Models.Party.NameLength} in total.", [nameof(Name), $"{nameof(profile)}[{count}].{nameof(profile.Name)}"]));
                    }
                }
            }

            if (ClaimTransforms?.Count() + ExitClaimTransforms?.Count() > Constants.Models.Claim.TransformsMax)
            {
                results.Add(new ValidationResult($"The number of claims transforms in '{nameof(ClaimTransforms)}' and '{nameof(ExitClaimTransforms)}' can be a  of {Constants.Models.Claim.TransformsMax} combined.", [nameof(ClaimTransforms), nameof(ExitClaimTransforms)]));
            }
            return results;
        }
    }
}
