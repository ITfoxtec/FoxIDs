using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class UserChangePasswordRequest : IValidatableObject
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

        /// <summary>
        /// The users current password
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        /// <summary>
        /// The users new password
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Email.IsNullOrEmpty() && Phone.IsNullOrEmpty() && Username.IsNullOrEmpty())
            {
                results.Add(new ValidationResult($"Either the field {nameof(Email)} or the field {nameof(Phone)} or the field {nameof(Username)} is required.", [nameof(Email), nameof(Phone), nameof(Username)]));
            }

            return results;
        }
    }
}
