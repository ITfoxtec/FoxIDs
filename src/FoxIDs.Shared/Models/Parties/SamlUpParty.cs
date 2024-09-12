using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ServiceModel.Security;
using System.Security.Cryptography.X509Certificates;
using ITfoxtec.Identity.Models;
using System.Linq;
using System;

namespace FoxIDs.Models
{
    public class SamlUpParty : UpPartyExternal<SamlUpPartyProfile>, ISamlClaimTransforms, IValidatableObject
    {
        public SamlUpParty()
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

        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [JsonProperty(PropertyName = "claim_transforms")]
        public List<SamlClaimTransform> ClaimTransforms { get; set; }

        [ListLength(Constants.Models.SamlParty.ClaimsMin, Constants.Models.SamlParty.ClaimsMax, Constants.Models.Claim.SamlTypeLength, Constants.Models.Claim.SamlTypeWildcardRegExPattern)]
        [JsonProperty(PropertyName = "claims")]
        public List<string> Claims { get; set; }

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

        [Required]
        [JsonProperty(PropertyName = "authn_binding")]
        public SamlBinding AuthnBinding { get; set; }

        [Required]
        [MaxLength(Constants.Models.SamlParty.Up.AuthnUrlLength)]
        [JsonProperty(PropertyName = "authn_url")]
        public string AuthnUrl { get; set; }

        [JsonProperty(PropertyName = "sign_authn_request")]
        public bool SignAuthnRequest { get; set; }

        [Required]
        [ListLength(Constants.Models.SamlParty.Up.KeysMin, Constants.Models.SamlParty.KeysMax)]
        [JsonProperty(PropertyName = "keys")]
        public List<JsonWebKey> Keys { get; set; }

        [ValidateComplexType]
        [JsonProperty(PropertyName = "logout_binding")]
        public SamlBinding LogoutBinding { get; set; }

        [MaxLength(Constants.Models.SamlParty.Up.LogoutUrlLength)]
        [JsonProperty(PropertyName = "logout_url")]
        public string LogoutUrl { get; set; }

        [MaxLength(Constants.Models.SamlParty.Up.LogoutUrlLength)]
        [JsonProperty(PropertyName = "single_logout_response_url")]
        public string SingleLogoutResponseUrl { get; set; }

        [JsonProperty(PropertyName = "authn_context_comparison")]
        public SamlAuthnContextComparisonTypes? AuthnContextComparison { get; set; }

        [ListLength(Constants.Models.SamlParty.Up.AuthnContextClassReferencesMin, Constants.Models.SamlParty.Up.AuthnContextClassReferencesMax, Constants.Models.Claim.LimitedValueLength)]
        [JsonProperty(PropertyName = "authn_context_class_refs")]
        public List<string> AuthnContextClassReferences { get; set; }

        [MaxLength(Constants.Models.SamlParty.Up.AuthnRequestExtensionsXmlLength)]
        [JsonProperty(PropertyName = "authn_request_extensions_xml")]
        public string AuthnRequestExtensionsXml { get; set; }

        [JsonProperty(PropertyName = "metadata_add_logout_response_location")]
        public bool MetadataAddLogoutResponseLocation { get; set; }

        [JsonProperty(PropertyName = "sign_metadata")]
        public bool SignMetadata { get; set; }

        [JsonProperty(PropertyName = "metadata_include_enc_certs")]
        public bool MetadataIncludeEncryptionCertificates { get; set; }

        [ListLength(Constants.Models.SamlParty.MetadataNameIdFormatsMin, Constants.Models.SamlParty.MetadataNameIdFormatsMax, Constants.Models.Claim.LimitedValueLength)]
        [JsonProperty(PropertyName = "metadata_nameid_formats")]
        public List<string> MetadataNameIdFormats { get; set; }

        [ListLength(Constants.Models.SamlParty.MetadataAttributeConsumingServicesMin, Constants.Models.SamlParty.MetadataAttributeConsumingServicesMax)]
        [JsonProperty(PropertyName = "metadata_attribute_consuming_service")]
        public List<SamlMetadataAttributeConsumingService> MetadataAttributeConsumingServices { get; set; }

        [ValidateComplexType]
        [JsonProperty(PropertyName = "metadata_organization")]
        public SamlMetadataOrganization MetadataOrganization { get; set; }

        [ListLength(Constants.Models.SamlParty.MetadataContactPersonsMin, Constants.Models.SamlParty.MetadataContactPersonsMax)]
        [JsonProperty(PropertyName = "metadata_contact_persons")]
        public List<SamlMetadataContactPerson> MetadataContactPersons { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            var baseResults = base.Validate(validationContext);
            if (baseResults.Count() > 0)
            {
                results.AddRange(baseResults);
            }

            if (Issuers?.Count() != 1)
            {
                results.Add(new ValidationResult($"Exactly one issuer in the field {nameof(Issuers)} is required.", [nameof(Issuers)]));
            }

            if (Claims?.Where(c => c == "*").Count() > 1)
            {
                results.Add(new ValidationResult($"Only one allow all wildcard (*) is allowed in the field {nameof(Claims)}.", [nameof(Claims)]));
            }
            return results;
        }
    }
}
