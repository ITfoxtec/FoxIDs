using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Schemas;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;

namespace FoxIDs.Models.Api
{
    public class SamlDownParty : IDownParty, INameValue, INewNameValue, IValidatableObject, IClaimTransformRef<SamlClaimTransform>
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

        /// <summary>
        /// Metadata update state.
        /// </summary>
        [Required]
        public PartyUpdateStates UpdateState { get; set; } = PartyUpdateStates.Manual;

        /// <summary>
        /// Metadata update rate in seconds.
        /// </summary>
        [Range(Constants.Models.SamlParty.MetadataUpdateRateMin, Constants.Models.SamlParty.MetadataUpdateRateMax)]
        public int? MetadataUpdateRate { get; set; } = 86400; // 24 hours

        /// <summary>
        /// Metadata URL.
        /// </summary>
        [MaxLength(Constants.Models.SamlParty.MetadataUrlLength)]
        public string MetadataUrl { get; set; }

        [Obsolete($"Please use {nameof(AllowUpParties)} instead.")]
        [ListLength(Constants.Models.DownParty.AllowUpPartyNamesMin, Constants.Models.DownParty.AllowUpPartyNamesMax, Constants.Models.Party.NameLength, Constants.Models.Party.NameRegExPattern)]
        public List<string> AllowUpPartyNames { get; set; }

        [ListLength(Constants.Models.DownParty.AllowUpPartyNamesMin, Constants.Models.DownParty.AllowUpPartyNamesMax)]
        public List<UpPartyLink> AllowUpParties { get; set; }

        /// <summary>
        /// Optional custom IdP issuer (default auto generated).
        /// </summary>
        [MaxLength(Constants.Models.Party.IssuerLength)]
        public string IdPIssuer { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<SamlClaimTransform> ClaimTransforms { get; set; }

        [ListLength(Constants.Models.SamlParty.ClaimsMin, Constants.Models.SamlParty.ClaimsMax, Constants.Models.Claim.SamlTypeLength, Constants.Models.Claim.SamlTypeWildcardRegExPattern)]
        public List<string> Claims { get; set; }

        /// <summary>
        /// Default 5 minutes.
        /// </summary>
        [Range(Constants.Models.SamlParty.Down.SubjectConfirmationLifetimeMin, Constants.Models.SamlParty.Down.SubjectConfirmationLifetimeMax)]
        public int? SubjectConfirmationLifetime { get; set; } = 300;

        /// <summary>
        /// Default 60 minutes.
        /// </summary>
        [Range(Constants.Models.SamlParty.Down.IssuedTokenLifetimeMin, Constants.Models.SamlParty.Down.IssuedTokenLifetimeMax)]
        public int? IssuedTokenLifetime { get; set; } = 3600;

        /// <summary>
        /// Default SHA256.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.SamlParty.SignatureAlgorithmLength)]
        public string SignatureAlgorithm { get; set; } = Saml2SecurityAlgorithms.RsaSha256Signature;

        /// <summary>
        /// URL binding pattern.
        /// </summary>
        public PartyBindingPatterns PartyBindingPattern { get; set; } = PartyBindingPatterns.Brackets;

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

        /// <summary>
        /// Default SignResponse.
        /// </summary>
        public Saml2AuthnResponseSignTypes AuthnResponseSignType { get; set; } = Saml2AuthnResponseSignTypes.SignResponse;

        [MaxLength(Constants.Models.Party.IssuerLength)]
        public string Issuer { get; set; }

        [Required]
        public SamlBindingTypes? AuthnRequestBinding { get; set; }

        [Required]
        public SamlBindingTypes? AuthnResponseBinding { get; set; }

        [ListLength(0, Constants.Models.SamlParty.Down.AcsUrlsMax, Constants.Models.SamlParty.Down.AcsUrlsLength)]
        public List<string> AcsUrls { get; set; }

        [Display(Name = "Disable absolute URLs")]
        public bool DisableAbsoluteUrls { get; set; }

        public bool EncryptAuthnResponse { get; set; }

        public string NameIdFormat { get; set; }

        public SamlBindingTypes? LogoutRequestBinding { get; set; } 

        public SamlBindingTypes? LogoutResponseBinding { get; set; } 

        [MaxLength(Constants.Models.SamlParty.Down.SingleLogoutUrlLength)]
        public string SingleLogoutUrl { get; set; }

        [MaxLength(Constants.Models.SamlParty.Down.LoggedOutUrlLength)]
        public string LoggedOutUrl { get; set; }

        [ListLength(Constants.Models.SamlParty.Down.KeysMin, Constants.Models.SamlParty.KeysMax)]
        public List<JwkWithCertificateInfo> Keys { get; set; }

        public JwkWithCertificateInfo EncryptionKey { get; set; }

        public bool MetadataAddLogoutResponseLocation { get; set; }

        public bool SignMetadata { get; set; }

        public bool MetadataIncludeEncryptionCertificates { get; set; }

        [ListLength(Constants.Models.SamlParty.MetadataNameIdFormatsMin, Constants.Models.SamlParty.MetadataNameIdFormatsMax, Constants.Models.Claim.LimitedValueLength, Constants.Models.Claim.SamlTypeRegExPattern)]
        public List<string> MetadataNameIdFormats { get; set; }

        [ValidateComplexType]
        public SamlMetadataOrganization MetadataOrganization { get; set; }

        [ListLength(Constants.Models.SamlParty.MetadataContactPersonsMin, Constants.Models.SamlParty.MetadataContactPersonsMax)]
        public List<SamlMetadataContactPerson> MetadataContactPersons { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Name.IsNullOrWhiteSpace() && DisplayName.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"Require either a Name or Display Name.", [nameof(Name), nameof(DisplayName)]));
            }
            if (!(AllowUpPartyNames?.Count > 0 || AllowUpParties?.Count > 0))
            {
                results.Add(new ValidationResult($"At least one in the field {nameof(AllowUpParties)} or {nameof(AllowUpPartyNames)} is required.", [nameof(AllowUpParties), nameof(AllowUpPartyNames)]));
            }

            if (Claims?.Where(c => c == "*").Count() > 1)
            {
                results.Add(new ValidationResult($"Only one wildcard (*) is allowed in the field {nameof(Claims)}.", [nameof(Claims)]));
            }

            if (UpdateState == PartyUpdateStates.Manual)
            {
                if (AcsUrls == null || AcsUrls.Count < Constants.Models.SamlParty.Down.AcsUrlsMin)
                {
                    results.Add(new ValidationResult($"The field {nameof(AcsUrls)} must be at least {Constants.Models.SamlParty.Down.AcsUrlsMin}.", new[] { nameof(AcsUrls) }));
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

            if (!LoggedOutUrl.IsNullOrWhiteSpace())
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

            if (AllowUpPartyNames?.Count() > 0 && AllowUpParties?.Count() > 0)
            {
                results.Add(new ValidationResult($"The field {nameof(AllowUpParties)} and the field {nameof(AllowUpPartyNames)} can not be used at the same time. Pleas only use the field {nameof(AllowUpParties)}.", [nameof(AllowUpParties), nameof(AllowUpPartyNames)]));
            }
            return results;
        }
    }
}
