using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ServiceModel.Security;
using System.Security.Cryptography.X509Certificates;
using ITfoxtec.Identity.Models;
using System.Linq;
using ITfoxtec.Identity.Saml2;

namespace FoxIDs.Models
{
    public class SamlDownParty : DownParty, ISamlClaimTransformsRef, IValidatableObject
    {
        public SamlDownParty()
        {
            Type = PartyTypes.Saml2;
        }

        [Required]
        [JsonProperty(PropertyName = "update_state")]
        public PartyUpdateStates UpdateState { get; set; } = PartyUpdateStates.Manual;

        [Range(Constants.Models.SamlParty.MetadataUpdateRateMin, Constants.Models.SamlParty.MetadataUpdateRateMax)]
        [JsonProperty(PropertyName = "metadata_update_rate")]
        public int? MetadataUpdateRate { get; set; }

        [MaxLength(Constants.Models.SamlParty.MetadataUrlLength)]
        [JsonProperty(PropertyName = "metadata_url")]
        public string MetadataUrl { get; set; }

        // Property can not be updated through API
        [Required]
        [JsonProperty(PropertyName = "last_updated")]
        public long LastUpdated { get; set; }

        [MaxLength(Constants.Models.Party.IssuerLength)]
        [JsonProperty(PropertyName = "idp_issuer")]
        public string IdPIssuer { get; set; }

        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [JsonProperty(PropertyName = "claim_transforms")]
        public List<SamlClaimTransform> ClaimTransforms { get; set; }

        [ListLength(Constants.Models.SamlParty.ClaimsMin, Constants.Models.SamlParty.ClaimsMax, Constants.Models.Claim.SamlTypeLength, Constants.Models.Claim.SamlTypeWildcardRegExPattern)]
        [JsonProperty(PropertyName = "claims")]
        public List<string> Claims { get; set; }

        [Range(Constants.Models.SamlParty.Down.SubjectConfirmationLifetimeMin, Constants.Models.SamlParty.Down.SubjectConfirmationLifetimeMax)]
        [JsonProperty(PropertyName = "subject_confirmation_lifetime")]
        public int SubjectConfirmationLifetime { get; set; }

        [Range(Constants.Models.SamlParty.Down.IssuedTokenLifetimeMin, Constants.Models.SamlParty.Down.IssuedTokenLifetimeMax)]
        [JsonProperty(PropertyName = "issued_token_lifetime")]
        public int IssuedTokenLifetime { get; set; }

        [Required]
        [MaxLength(Constants.Models.SamlParty.SignatureAlgorithmLength)]
        [JsonProperty(PropertyName = "signature_algorithm")]
        public string SignatureAlgorithm { get; set; }

        [Required]
        [JsonProperty(PropertyName = "certificate_validation_mode")]
        public X509CertificateValidationMode CertificateValidationMode { get; set; }

        [Required]
        [JsonProperty(PropertyName = "revocation_mode")]
        public X509RevocationMode RevocationMode { get; set; }

        [JsonProperty(PropertyName = "authn_response_sign_type")]
        public Saml2AuthnResponseSignTypes AuthnResponseSignType { get; set; } = Saml2AuthnResponseSignTypes.SignResponse;

        [Required]
        [MaxLength(Constants.Models.Party.IssuerLength)]
        [JsonProperty(PropertyName = "issuer")]
        public string Issuer { get; set; }

        [Required]
        [JsonProperty(PropertyName = "authn_binding")]
        public SamlBinding AuthnBinding { get; set; }

        [ListLength(Constants.Models.SamlParty.Down.AcsUrlsMin, Constants.Models.SamlParty.Down.AcsUrlsMax, Constants.Models.SamlParty.Down.AcsUrlsLength)]
        [JsonProperty(PropertyName = "acs_urls")]
        public List<string> AcsUrls { get; set; }

        [JsonProperty(PropertyName = "disable_absolute_urls")]
        public bool DisableAbsoluteUrls { get; set; }

        [JsonProperty(PropertyName = "encrypt_authn_response")]
        public bool EncryptAuthnResponse { get; set; }

        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [RegularExpression(Constants.Models.Claim.SamlTypeRegExPattern)]
        [JsonProperty(PropertyName = "nameid_format")]
        public string NameIdFormat { get; set; }

        [ValidateComplexType]
        [JsonProperty(PropertyName = "logout_binding")]
        public SamlBinding LogoutBinding { get; set; }

        [MaxLength(Constants.Models.SamlParty.Down.SingleLogoutUrlLength)]
        [JsonProperty(PropertyName = "single_logout_url")]
        public string SingleLogoutUrl { get; set; }

        [MaxLength(Constants.Models.SamlParty.Down.LoggedOutUrlLength)]
        [JsonProperty(PropertyName = "logged_out_url")]
        public string LoggedOutUrl { get; set; }

        [ListLength(Constants.Models.SamlParty.Down.KeysMin, Constants.Models.SamlParty.KeysMax)]
        [JsonProperty(PropertyName = "keys")]
        public List<JsonWebKey> Keys { get; set; }

        [JsonProperty(PropertyName = "encryption_key")]
        public JsonWebKey EncryptionKey { get; set; }

        [JsonProperty(PropertyName = "metadata_add_logout_response_location")]
        public bool MetadataAddLogoutResponseLocation { get; set; }

        [JsonProperty(PropertyName = "sign_metadata")]
        public bool SignMetadata { get; set; }

        [JsonProperty(PropertyName = "metadata_include_enc_certs")]
        public bool MetadataIncludeEncryptionCertificates { get; set; }

        [ListLength(Constants.Models.SamlParty.MetadataNameIdFormatsMin, Constants.Models.SamlParty.MetadataNameIdFormatsMax, Constants.Models.Claim.LimitedValueLength, Constants.Models.Claim.SamlTypeRegExPattern)]
        [JsonProperty(PropertyName = "metadata_nameid_formats")]
        public List<string> MetadataNameIdFormats { get; set; }

        [ValidateComplexType]
        [JsonProperty(PropertyName = "metadata_organization")]
        public SamlMetadataOrganization MetadataOrganization { get; set; }

        [ListLength(Constants.Models.SamlParty.MetadataContactPersonsMin, Constants.Models.SamlParty.MetadataContactPersonsMax)]
        [JsonProperty(PropertyName = "metadata_contact_persons")]
        public List<SamlMetadataContactPerson> MetadataContactPersons { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (!(AllowUpParties?.Where(up => !up.DisableUserAuthenticationTrust)?.Count() > 0))
            {
                results.Add(new ValidationResult($"At least one (with user authentication trust) in the field {nameof(AllowUpParties)} is required.", [nameof(AllowUpParties)]));
            }

            if (Claims?.Where(c => c == "*").Count() > 1)
            {
                results.Add(new ValidationResult($"Only one allow all wild card (*) is allowed in the field {nameof(Claims)}.", [nameof(Claims)]));
            }
            return results;
        }
    }
}
