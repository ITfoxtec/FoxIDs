using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Stored password hash used to enforce history policies.
    /// </summary>
    public class PasswordHistoryItem : IValidatableObject
    {
        /// <summary>
        /// Hash algorithm used for the stored password hash.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.SecretHash.HashAlgorithmLength)]
        public string HashAlgorithm { get; set; }

        /// <summary>
        /// Password hash value.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.SecretHash.HashLength)]
        public string Hash { get; set; }

        /// <summary>
        /// Salt used to generate the hash.
        /// </summary>
        [MaxLength(Constants.Models.SecretHash.HashSaltLength)]
        public string HashSalt { get; set; }

        /// <summary>
        /// Unix time the password was set.
        /// </summary>
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