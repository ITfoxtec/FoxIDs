﻿using ITfoxtec.Identity.Discovery;
using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.IdentityModel.Tokens;
using System.ServiceModel.Security;
using System.Security.Cryptography.X509Certificates;

namespace FoxIDs.Models
{
    public class SamlDownParty : DownParty, IValidatableObject
    {
        public SamlDownParty()
        {
            Type = PartyTypes.Saml2;
        }

        [MaxLength(Constants.Models.SamlParty.IssuerLength)]
        [JsonProperty(PropertyName = "ids_issuer")]
        public string IdSIssuer { get; set; }

        [Length(Constants.Models.Party.ClaimTransformationClaimsMin, Constants.Models.Party.ClaimTransformationClaimsMax)]
        [JsonProperty(PropertyName = "claim_transformations")]
        public List<SamlClaimTransformation> ClaimTransformations { get; set; }

        [Length(Constants.Models.SamlParty.ClaimsMin, Constants.Models.SamlParty.ClaimsMax, Constants.Models.SamlParty.ClaimLength)]
        [JsonProperty(PropertyName = "claims")]
        public List<string> Claims { get; set; }

        [Range(Constants.Models.SamlParty.MetadataLifetimeMin, Constants.Models.SamlParty.MetadataLifetimeMax)]
        [JsonProperty(PropertyName = "metadata_lifetime")]
        public int MetadataLifetime { get; set; }

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

        [Required]
        [MaxLength(Constants.Models.SamlParty.IssuerLength)]
        [JsonProperty(PropertyName = "issuer")]
        public string Issuer { get; set; }

        [Required]
        [JsonProperty(PropertyName = "authn_binding")]
        public SamlBinding AuthnBinding { get; set; }

        [Length(Constants.Models.SamlParty.Down.AcsUrlsMin, Constants.Models.SamlParty.Down.AcsUrlsMax, Constants.Models.SamlParty.Down.AcsUrlsLength)]
        [JsonProperty(PropertyName = "acs_urls")]
        public List<string> AcsUrls { get; set; }

        [ValidateComplexType]
        [JsonProperty(PropertyName = "logout_binding")]
        public SamlBinding LogoutBinding { get; set; }

        [MaxLength(Constants.Models.SamlParty.Down.SingleLogoutUrlLength)]
        [JsonProperty(PropertyName = "single_logout_url")]
        public string SingleLogoutUrl { get; set; }

        [MaxLength(Constants.Models.SamlParty.Down.LoggedOutUrlLength)]
        [JsonProperty(PropertyName = "logged_out_url")]
        public string LoggedOutUrl { get; set; }

        [Length(Constants.Models.SamlParty.Down.KeysMin, Constants.Models.SamlParty.KeysMax)]
        [JsonProperty(PropertyName = "keys")]
        public List<JsonWebKey> Keys { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (AllowUpParties?.Count <= 0)
            {
                results.Add(new ValidationResult($"At least one in the field {nameof(AllowUpParties)} is required.", new[] { nameof(AllowUpParties) }));
            }
            return results;
        }
    }
}
