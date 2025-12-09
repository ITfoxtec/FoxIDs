using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Request to set a user's password without the current password.
    /// </summary>
    public class UserSetPasswordRequest : IValidatableObject
    {
        /// <summary>
        /// The users email address.
        /// </summary>
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        public string Email { get; set; }

        /// <summary>
        /// The users phone number.
        /// </summary>
        [MaxLength(Constants.Models.User.PhoneLength)]
        [RegularExpression(Constants.Models.User.PhoneRegExPattern)]
        public string Phone { get; set; }

        /// <summary>
        /// The users username.
        /// </summary>
        [MaxLength(Constants.Models.User.UsernameLength)]
        [RegularExpression(Constants.Models.User.UsernameRegExPattern)]
        public string Username { get; set; }

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

        [Display(Name = "Require password change")]
        public bool ChangePassword { get; set; }

        [Range(0, long.MaxValue)]
        [Display(Name = "Password last changed (Unix seconds)")]
        public long? PasswordLastChanged { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Email.IsNullOrEmpty() && Phone.IsNullOrEmpty() && Username.IsNullOrEmpty())
            {
                results.Add(new ValidationResult($"Either the field {nameof(Email)} or the field {nameof(Phone)} or the field {nameof(Username)} is required.", [nameof(Email), nameof(Phone), nameof(Username)]));
            }

            var passwordProvided = !Password.IsNullOrWhiteSpace();
            var passwordHashProvided = !PasswordHashAlgorithm.IsNullOrWhiteSpace() || !PasswordHash.IsNullOrWhiteSpace() || !PasswordHashSalt.IsNullOrWhiteSpace();

            if (passwordProvided && passwordHashProvided)
            {
                results.Add(new ValidationResult($"Provide either the {nameof(Password)} field or the {nameof(PasswordHashAlgorithm)}, {nameof(PasswordHash)} and {nameof(PasswordHashSalt)} fields.", [nameof(Password), nameof(PasswordHashAlgorithm), nameof(PasswordHash), nameof(PasswordHashSalt)]));
            }
            if (passwordHashProvided && PasswordHashAlgorithm.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field {nameof(PasswordHashAlgorithm)} is required when setting a hashed password.", [nameof(PasswordHashAlgorithm)]));
            }
            if (!PasswordHashAlgorithm.IsNullOrWhiteSpace())
            {
                if (PasswordHashAlgorithm != Constants.Models.SecretHash.DefaultHashAlgorithm)
                {
                    results.Add(new ValidationResult($"Hash algorithm in field {nameof(PasswordHashAlgorithm)} not supported. Supported hash algorithm '{Constants.Models.SecretHash.DefaultHashAlgorithm}'.", [nameof(PasswordHashAlgorithm)]));
                }

                if (PasswordHash.IsNullOrWhiteSpace() || PasswordHashSalt.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field {nameof(PasswordHash)} and the field {nameof(PasswordHashSalt)} is required.", [nameof(PasswordHashAlgorithm), nameof(PasswordHash), nameof(PasswordHashSalt)]));
                }
            }

            return results;
        }
    }
}
