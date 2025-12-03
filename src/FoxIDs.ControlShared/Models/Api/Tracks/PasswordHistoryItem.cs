using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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

        public long Created { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!HashAlgorithm.Equals("SHA256", System.StringComparison.Ordinal))
            {
                yield return new ValidationResult($"Unsupported {nameof(HashAlgorithm)} '{HashAlgorithm}'. Only 'SHA256' is supported for password history.", [nameof(HashAlgorithm)]);
            }
        }
    }
}