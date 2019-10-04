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
    public class SamlUpParty : UpParty
    {
        public SamlUpParty()
        {
            Type = PartyType.Saml2;
        }

        [MaxLength(Constants.Models.SamlUpParty.IssuerLength)]
        [JsonProperty(PropertyName = "ids_issuer")]
        public string IdSIssuer { get; set; }

        [Range(Constants.Models.SamlUpParty.MetadataLifetimeMin, Constants.Models.SamlUpParty.MetadataLifetimeMax)] 
        [JsonProperty(PropertyName = "metadata_lifetime")]
        public int MetadataLifetime { get; set; }

        [Required]
        [MaxLength(Constants.Models.SamlUpParty.SignatureAlgorithmLength)]
        [JsonProperty(PropertyName = "signature_algorithm")]
        public string SignatureAlgorithm { get; set; }

        [Required]
        [JsonProperty(PropertyName = "certificate_validation_mode")]
        public X509CertificateValidationMode CertificateValidationMode { get; set; }

        [Required]
        [JsonProperty(PropertyName = "revocation_mode")]
        public X509RevocationMode RevocationMode { get; set; }

        [Required]
        [MaxLength(Constants.Models.SamlUpParty.IssuerLength)]
        [JsonProperty(PropertyName = "issuer")]
        public string Issuer { get; set; }

        [Required]
        [JsonProperty(PropertyName = "authn_binding")]
        public SamlBinding AuthnBinding { get; set; }

        [Required]
        [MaxLength(Constants.Models.SamlUpParty.AuthnUrlLength)]
        [JsonProperty(PropertyName = "authn_url")]
        public string AuthnUrl { get; set; }

        [Required]
        [Length(Constants.Models.SamlUpParty.KeysMin, Constants.Models.SamlUpParty.KeysMax)]
        [JsonProperty(PropertyName = "keys")]
        public List<JsonWebKey> Keys { get; set; }

        [ValidateObject]
        [JsonProperty(PropertyName = "logout_binding")]
        public SamlBinding LogoutBinding { get; set; }

        [MaxLength(Constants.Models.SamlUpParty.LogoutUrlLength)]
        [JsonProperty(PropertyName = "logout_url")]
        public string LogoutUrl { get; set; }
    }
}
