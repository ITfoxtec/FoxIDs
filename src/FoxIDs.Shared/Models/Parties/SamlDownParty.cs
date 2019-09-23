using ITfoxtec.Identity.Discovery;
using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.IdentityModel.Tokens;
using System.ServiceModel.Security;
using System.Security.Cryptography.X509Certificates;

namespace FoxIDs.Models
{
    public class SamlDownParty : DownParty
    {
        public SamlDownParty()
        {
            Type = PartyType.Saml2;
        }

        [MaxLength(300)]
        [JsonProperty(PropertyName = "ids_issuer")]
        public string IdSIssuer { get; set; }

        [Length(0, 500, 500)]
        [JsonProperty(PropertyName = "claims")]
        public List<string> Claims { get; set; }

        [Range(86400, 31536000)] // 24 hours to 12 month
        [JsonProperty(PropertyName = "metadata_lifetime")]
        public int MetadataLifetime { get; set; }

        [Range(60, 900)] // 1 minutes to 15 minutes
        [JsonProperty(PropertyName = "subject_confirmation_lifetime")]
        public int SubjectConfirmationLifetime { get; set; }

        [Range(300, 86400)] // 5 minutes to 24 hours
        [JsonProperty(PropertyName = "issued_token_lifetime")]
        public int IssuedTokenLifetime { get; set; }

        [Required]
        [MaxLength(100)]
        [JsonProperty(PropertyName = "signature_algorithm")]
        public string SignatureAlgorithm { get; set; }

        [Required]
        [JsonProperty(PropertyName = "certificate_validation_mode")]
        public X509CertificateValidationMode CertificateValidationMode { get; set; }

        [Required]
        [JsonProperty(PropertyName = "revocation_mode")]
        public X509RevocationMode RevocationMode { get; set; }

        [Required]
        [MaxLength(300)]
        [JsonProperty(PropertyName = "issuer")]
        public string Issuer { get; set; }

        [Required]
        [JsonProperty(PropertyName = "authn_binding")]
        public SamlBinding AuthnBinding { get; set; }

        [Length(1, 10, 500)]
        [JsonProperty(PropertyName = "acs_urls")]
        public List<string> AcsUrls { get; set; }

        [ValidateObject]
        [JsonProperty(PropertyName = "logout_binding")]
        public SamlBinding LogoutBinding { get; set; }

        [MaxLength(500)]
        [JsonProperty(PropertyName = "single_logout_url")]
        public string SingleLogoutUrl { get; set; }

        [MaxLength(500)]
        [JsonProperty(PropertyName = "logged_out_url")]
        public string LoggedOutUrl { get; set; }

        [Length(0, 10)]
        [JsonProperty(PropertyName = "keys")]
        public List<JsonWebKey> Keys { get; set; }
    }
}
