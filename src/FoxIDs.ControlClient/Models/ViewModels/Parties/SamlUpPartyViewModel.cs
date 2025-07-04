﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity.Saml2.Schemas;
using System.ServiceModel.Security;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using System.Linq;

namespace FoxIDs.Client.Models.ViewModels
{
    public class SamlUpPartyViewModel : IValidatableObject, IUpPartySessionLifetime, IUpPartyHrd, ISamlMetadataAttributeConsumingServiceVievModel, ISamlMetadataOrganizationVievModel, ISamlMetadataContactPersonVievModel
    {
        public string InitName { get; set; }

        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Technical name")]
        public string Name { get; set; }
        
        [Required]
        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        [Display(Name = "Name")]
        public string DisplayName { get; set; }

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

        [MaxLength(Constants.Models.Party.IssuerLength)]
        [Display(Name = "Optional custom SP issuer / audience (default auto generated)")]
        public string SpIssuer { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<ClaimTransformViewModel> ClaimTransforms { get; set; } = new List<ClaimTransformViewModel>();

        /// <summary>
        /// Extended UIs.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.ExtendedUi.UisMin, Constants.Models.ExtendedUi.UisMax)]
        public List<ExtendedUiViewModel> ExtendedUis { get; set; } = new List<ExtendedUiViewModel>();

        /// <summary>
        /// Claim transforms executed after the external users claims has been loaded.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<ClaimTransformViewModel> ExitClaimTransforms { get; set; } = new List<ClaimTransformViewModel>();

        [ValidateComplexType]
        [ListLength(Constants.Models.SamlParty.ClaimsMin, Constants.Models.SamlParty.ClaimsMax, Constants.Models.Claim.SamlTypeLength, Constants.Models.Claim.SamlTypeWildcardRegExPattern)]
        [Display(Name = "Forward claims (use * to carried all claims forward)")]
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
        [Display(Name = "Certificate revocation mode")]
        public X509RevocationMode RevocationMode { get; set; } = X509RevocationMode.NoCheck;

        [MaxLength(Constants.Models.Party.IssuerLength)]
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
        [ListLength(0, Constants.Models.SamlParty.KeysMax)]
        [Display(Name = "One or more signature validation certificates")]
        public List<JwkWithCertificateInfo> Keys { get; set; }

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
        /// URL binding pattern.
        /// </summary>
        [Display(Name = "URL binding pattern")]
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
        public bool DisableSingleLogout { get; set; } 

        [Display(Name = "Optional Authn context comparison")]
        public SamlAuthnContextComparisonTypesVievModel AuthnContextComparisonViewModel { get; set; }

        [Display(Name = "Optional Authn context class references")]
        public List<string> AuthnContextClassReferences { get; set; } = new List<string>();

        [Display(Name = "Optional Authn request extensions XML")]
        public string AuthnRequestExtensionsXml { get; set; }

        [Display(Name = "Login hint in Authn request in Subject NameID")]
        public bool DisableLoginHint { get; set; }

        [Display(Name = "Add logout response location URL in metadata")]
        public bool MetadataAddLogoutResponseLocation { get; set; }

        [Display(Name = "Sign metadata")]
        public bool SignMetadata { get; set; }

        [Display(Name = "Include encryption certificates in metadata")]
        public bool MetadataIncludeEncryptionCertificates { get; set; }

        [ListLength(Constants.Models.SamlParty.MetadataNameIdFormatsMin, Constants.Models.SamlParty.MetadataNameIdFormatsMax, Constants.Models.Claim.LimitedValueLength, Constants.Models.Claim.SamlTypeRegExPattern)]
        [Display(Name = "Optional NameID formats in metadata")]
        public List<string> MetadataNameIdFormats { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.SamlParty.MetadataAttributeConsumingServicesMin, Constants.Models.SamlParty.MetadataAttributeConsumingServicesMax)]
        public List<SamlMetadataAttributeConsumingService> MetadataAttributeConsumingServices { get; set; } = new List<SamlMetadataAttributeConsumingService>();

        [ValidateComplexType]
        public SamlMetadataOrganization MetadataOrganization { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.SamlParty.MetadataContactPersonsMin, Constants.Models.SamlParty.MetadataContactPersonsMax)]
        public List<SamlMetadataContactPerson> MetadataContactPersons { get; set; } = new List<SamlMetadataContactPerson>();

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

        [Display(Name = "User authentication trust")]
        public bool DisableUserAuthenticationTrust { get; set; }

        [Display(Name = "Token exchange trust")]
        public bool DisableTokenExchangeTrust { get; set; }

        [Display(Name = "IdP-Initiated Login")]
        public bool EnableIdPInitiated { get; set; }

        [Range(-1, Constants.Models.SamlParty.Up.IdPInitiatedGrantLifetimeMax)]
        [Display(Name = "IdP-Initiated Login grant lifetime for OpenID Connect applications (active if greater than 0)")]
        public int? IdPInitiatedGrantLifetime { get; set; }

        [ValidateComplexType]
        public LinkExternalUserViewModel LinkExternalUser { get; set; } = new LinkExternalUserViewModel();

        [ValidateComplexType]
        [ListLength(Constants.Models.UpParty.ProfilesMin, Constants.Models.UpParty.ProfilesMax)]
        public List<SamlUpPartyProfileViewModel> Profiles { get; set; } = new List<SamlUpPartyProfileViewModel>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (DisableUserAuthenticationTrust && DisableTokenExchangeTrust)
            {
                results.Add(new ValidationResult($"Both the {nameof(DisableUserAuthenticationTrust)} and the {nameof(DisableTokenExchangeTrust)} can not be disabled at the same time.", new[] { nameof(DisableUserAuthenticationTrust), nameof(DisableTokenExchangeTrust) }));
            }

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

            if (ClaimTransforms?.Count() + ExitClaimTransforms?.Count() > Constants.Models.Claim.TransformsMax)
            {
                results.Add(new ValidationResult($"The number of claims transforms in '{nameof(ClaimTransforms)}' and '{nameof(ExitClaimTransforms)}' can be a  of {Constants.Models.Claim.TransformsMax} combined.", [nameof(ClaimTransforms), nameof(ExitClaimTransforms)]));
            }
            return results;
        }
    }
}
