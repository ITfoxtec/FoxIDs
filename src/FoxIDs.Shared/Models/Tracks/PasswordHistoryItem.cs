using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class PasswordHistoryItem : IValidatableObject, ISecretHash
    {
        [Required]
        [MaxLength(Constants.Models.SecretHash.HashAlgorithmLength)]
        [JsonProperty(PropertyName = "hash_algorithm")]
        public string HashAlgorithm { get; set; }

        [Required]
        [MaxLength(Constants.Models.SecretHash.HashLength)]
        [JsonProperty(PropertyName = "hash")]
        public string Hash { get; set; }

        [MaxLength(Constants.Models.SecretHash.HashSaltLength)]
        [JsonProperty(PropertyName = "hash_salt")]
        public string HashSalt { get; set; }

        [JsonProperty(PropertyName = "created")]
        public long Created { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var isDefaultHash = HashAlgorithm.StartsWith(Constants.Models.SecretHash.DefaultPostHashAlgorithm, StringComparison.Ordinal);
            if (isDefaultHash && string.IsNullOrWhiteSpace(HashSalt))
            {
                yield return new ValidationResult($"The field {nameof(HashSalt)} is required for hash algorithm '{HashAlgorithm}'.", [nameof(HashSalt)]);
            }
        }
    }
}
