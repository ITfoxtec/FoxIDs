using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ServiceModel.Security;
using System.Security.Cryptography.X509Certificates;
using ITfoxtec.Identity.Models;

namespace FoxIDs.Models
{
    public class SamlUpParty : UpParty
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

        [MaxLength(Constants.Models.SamlParty.IssuerLength)]
        [JsonProperty(PropertyName = "sp_issuer")]
        public string SpIssuer { get; set; }

        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [JsonProperty(PropertyName = "claim_transforms")]
        public List<SamlClaimTransform> ClaimTransforms { get; set; }

        [Length(Constants.Models.SamlParty.ClaimsMin, Constants.Models.SamlParty.ClaimsMax, Constants.Models.Claim.SamlTypeLength, Constants.Models.Claim.SamlTypeRegExPattern)]
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
        [MaxLength(Constants.Models.SamlParty.IssuerLength)]
        [JsonProperty(PropertyName = "issuer")]
        public string Issuer { get; set; }

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
        [Length(Constants.Models.SamlParty.Up.KeysMin, Constants.Models.SamlParty.KeysMax)]
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

        [JsonProperty(PropertyName = "disable_sign_metadata")]
        public bool DisableSignMetadata { get; set; }
    }
}
