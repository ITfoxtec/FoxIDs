using ITfoxtec.Identity;
using ITfoxtec.Identity.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ClientKey : IValidatableObject
    {
        [Required]
        [JsonProperty(PropertyName = "type")]
        public ClientKeyTypes Type { get; set; }

        [Required]
        [JsonProperty(PropertyName = "external_name")]
        public string ExternalName { get; set; }

        [JsonProperty(PropertyName = "key")]
        public JsonWebKey Key { get; set; }

        [Required]
        [JsonProperty(PropertyName = "public_key")]
        public JsonWebKey PublicKey { get; set; }

        [JsonProperty(PropertyName = "external_id")]
        public string ExternalId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Type == ClientKeyTypes.KeyVaultImport && ExternalId.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field {nameof(ExternalId)} is required for key type '{Type}'.", [nameof(ExternalId)]));
            }           

            if (Type == ClientKeyTypes.Contained && Key == null)
            {
                results.Add(new ValidationResult($"The field {nameof(Key)} is required for key type '{Type}'.", [nameof(Key)]));
            }

            return results;
        }
    }
}
