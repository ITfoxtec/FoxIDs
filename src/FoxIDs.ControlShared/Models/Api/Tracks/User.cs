using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class User : IEmailValue, IValidatableObject
    {
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [MaxLength(Constants.Models.User.PhoneLength)]
        [RegularExpression(Constants.Models.User.PhoneRegExPattern)]
        public string Phone { get; set; }

        [MaxLength(Constants.Models.User.UsernameLength)]
        [RegularExpression(Constants.Models.User.UsernameRegExPattern)]
        public string Username { get; set; }

        [Display(Name = "User must confirm account")]
        public bool ConfirmAccount { get; set; }

        [Display(Name = "Email verified")]
        public bool EmailVerified { get; set; }

        [Display(Name = "User must change password")]
        public bool ChangePassword { get; set; }

        [Required]
        [MaxLength(Constants.Models.User.UserIdLength)]
        [Display(Name = "User id (unique and persistent)")]
        public string UserId { get; set; }

        [Display(Name = "Disable account")]
        public bool DisableAccount { get; set; }

        [Display(Name = "Active two-factor (2FA) app")]
        public bool ActiveTwoFactorApp { get; set; }

        [Display(Name = "Require multi-factor (2FA/MFA)")]
        public bool RequireMultiFactor { get; set; }

        [ListLength(Constants.Models.User.ClaimsMin, Constants.Models.User.ClaimsMax)]
        [Display(Name = "Claims")]
        public List<ClaimAndValues> Claims { get; set; }

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
