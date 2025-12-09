using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Request to create a single user.
    /// </summary>
    public class CreateUserRequest : UserBase, IValidatableObject
    {
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [MaxLength(Constants.Models.SecretHash.HashAlgorithmLength)]
        [Display(Name = "Password hash algorithm")]
        public string PasswordHashAlgorithm { get; set; }

        [MaxLength(Constants.Models.SecretHash.HashLength)]
        [Display(Name = "Password hash")]
        public string PasswordHash { get; set; }

        [MaxLength(Constants.Models.SecretHash.HashSaltLength)]
        [Display(Name = "Password hash salt")]
        public string PasswordHashSalt { get; set; }

        [Range(0, long.MaxValue)]
        [Display(Name = "Password last changed (Unix seconds)")]
        public long? PasswordLastChanged { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Password.IsNullOrWhiteSpace() && (!PasswordHashAlgorithm.IsNullOrWhiteSpace() || !PasswordHash.IsNullOrWhiteSpace() || !PasswordHashSalt.IsNullOrWhiteSpace()))
            {
                yield return new ValidationResult($"Provide either the {nameof(Password)} field or the {nameof(PasswordHashAlgorithm)}, {nameof(PasswordHash)} and {nameof(PasswordHashSalt)} fields.", [nameof(Password), nameof(PasswordHashAlgorithm), nameof(PasswordHash), nameof(PasswordHashSalt)]);
            }
            if (!PasswordHashAlgorithm.IsNullOrWhiteSpace())
            {
                if (PasswordHashAlgorithm != Constants.Models.SecretHash.DefaultHashAlgorithm)
                {
                    yield return new ValidationResult($"Hash algorithm in field {nameof(PasswordHashAlgorithm)} not supported. Supported hash algorithm '{Constants.Models.SecretHash.DefaultHashAlgorithm}'.", [nameof(PasswordHashAlgorithm)]);
                }

                if (PasswordHash.IsNullOrWhiteSpace() || PasswordHashSalt.IsNullOrWhiteSpace())
                {
                    yield return new ValidationResult($"The field {nameof(PasswordHash)} and the field {nameof(PasswordHashSalt)} is required.", [nameof(PasswordHashAlgorithm), nameof(PasswordHash), nameof(PasswordHashSalt)]);
                }
            }
        }
    }
}
