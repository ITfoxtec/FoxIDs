using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;

namespace FoxIDs.Models.Api
{
    public class PasswordHistoryItem : IValidatableObject
    {
        [Required]
        [MaxLength(Constants.Models.SecretHash.HashAlgorithmLength)]
        public string HashAlgorithm { get; set; }

        [Required]
        [MaxLength(Constants.Models.SecretHash.HashLength)]
        public string Hash { get; set; }

        [MaxLength(Constants.Models.SecretHash.HashSaltLength)]
        public string HashSalt { get; set; }

        public long Created { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var isDefaultHash = HashAlgorithm.StartsWith(Constants.Models.SecretHash.DefaultPostHashAlgorithm, StringComparison.Ordinal);
            var isHistoryHash = HashAlgorithm.Equals(Constants.Models.SecretHash.PasswordHistoryHashAlgorithm, StringComparison.Ordinal);
            if (!isDefaultHash && !isHistoryHash)
            {
                yield return new ValidationResult($"Unsupported {nameof(HashAlgorithm)} '{HashAlgorithm}'. Only '{Constants.Models.SecretHash.DefaultPostHashAlgorithm}' based or '{Constants.Models.SecretHash.PasswordHistoryHashAlgorithm}' hashes are supported for password history.", [nameof(HashAlgorithm)]);
            }

            if (isDefaultHash && string.IsNullOrWhiteSpace(HashSalt))
            {
                yield return new ValidationResult($"The field {nameof(HashSalt)} is required for hash algorithm '{HashAlgorithm}'.", [nameof(HashSalt)]);
            }
        }
    }
}