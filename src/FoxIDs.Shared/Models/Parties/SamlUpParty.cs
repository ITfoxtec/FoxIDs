using ITfoxtec.Identity.Discovery;
using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.IdentityModel.Tokens;

namespace FoxIDs.Models
{
    public class SamlUpParty : UpParty
    {
        public SamlUpParty()
        {
            Type = PartyType.Saml2.ToString();
        }

        [MaxLength(300)]
        [JsonProperty(PropertyName = "ids_issuer")]
        public string IdSIssuer { get; set; }

        [Range(86400, 31536000)] // 24 hours to 12 month
        [JsonProperty(PropertyName = "metadata_lifetime")]
        public int MetadataLifetime { get; set; }

        [Required]
        [MaxLength(100)]
        [JsonProperty(PropertyName = "signature_algorithm")]
        public string SignatureAlgorithm { get; set; }

        [Required]
        [MaxLength(30)]
        [JsonProperty(PropertyName = "certificate_validation_mode")]
        public string CertificateValidationMode { get; set; }

        [Required]
        [MaxLength(30)]
        [JsonProperty(PropertyName = "revocation_mode")]
        public string RevocationMode { get; set; }

        [Required]
        [MaxLength(300)]
        [JsonProperty(PropertyName = "issuer")]
        public string Issuer { get; set; }

        [Required]
        [JsonProperty(PropertyName = "authn_binding")]
        public SamlBinding AuthnBinding { get; set; }

        [Required]
        [MaxLength(500)]
        [JsonProperty(PropertyName = "authn_url")]
        public string AuthnUrl { get; set; }

        [Required]
        [Length(1, 10)]
        [JsonProperty(PropertyName = "keys")]
        public List<JsonWebKey> Keys { get; set; }

        [ValidateObject]
        [JsonProperty(PropertyName = "logout_binding")]
        public SamlBinding LogoutBinding { get; set; }

        [MaxLength(500)]
        [JsonProperty(PropertyName = "logout_url")]
        public string LogoutUrl { get; set; }
    }
}
