using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class UserBase : IValidatableObject
    {
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [MaxLength(Constants.Models.User.PhoneLength)]
        [RegularExpression(Constants.Models.User.PhoneRegExPattern)]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        [MaxLength(Constants.Models.User.UsernameLength)]
        [RegularExpression(Constants.Models.User.UsernameRegExPattern)]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Display(Name = "User must confirm account")]
        public bool ConfirmAccount { get; set; }

        [Display(Name = "Email verified")]
        public bool EmailVerified { get; set; }

        [Display(Name = "Phone verified")]
        public bool PhoneVerified { get; set; }

        [Display(Name = "User must change password")]
        public bool ChangePassword { get; set; }

        [Display(Name = "User cannot set / reset password with SMS")]
        public bool DisableSetPasswordSms { get; set; }

        [Display(Name = "User cannot set / reset password with email")]
        public bool DisableSetPasswordEmail { get; set; }

        /// <summary>
        /// SetPassword with email require the user to have a email user identifier.
        /// </summary>
        [Display(Name = "Require set password with email confirmation")]
        public bool SetPasswordEmail { get; set; }

        /// <summary>
        /// SetPassword with SMS require the user to have a phone user identifier.
        /// </summary>
        [Display(Name = "Require set password with phone confirmation")]
        public bool SetPasswordSms { get; set; }

        [Display(Name = "Disable account")]
        public bool DisableAccount { get; set; }

        [Display(Name = "Two-factor authenticator app disabled")]
        public bool DisableTwoFactorApp { get; set; }

        [Display(Name = "Two-factor with SMS disabled")]
        public bool DisableTwoFactorSms { get; set; }

        [Display(Name = "Two-factor with email disabled")]
        public bool DisableTwoFactorEmail { get; set; }

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

            if (SetPasswordEmail)
            {
                if (DisableSetPasswordEmail)
                {
                    results.Add(new ValidationResult($"Set password with email is disabled.", [nameof(DisableSetPasswordEmail), nameof(SetPasswordEmail)]));
                }
                if (Email.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The field {nameof(Email)} is required to set password with email.", [nameof(Email), nameof(SetPasswordEmail)]));
                }
            }
            if (SetPasswordSms)
            {
                if (DisableSetPasswordSms)
                {
                    results.Add(new ValidationResult($"Set password with SMS is disabled.", [nameof(DisableSetPasswordSms), nameof(SetPasswordSms)]));
                }
                if (Phone.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The field {nameof(Phone)} is required to set password with SMS.", [nameof(Phone), nameof(SetPasswordSms)]));
                }
            }

            if (RequireMultiFactor && DisableTwoFactorApp && DisableTwoFactorSms && DisableTwoFactorEmail)
            {
                results.Add(new ValidationResult($"Either the field {nameof(DisableTwoFactorApp)} or the field {nameof(DisableTwoFactorSms)} or the field {nameof(DisableTwoFactorEmail)} should be False if the field {nameof(RequireMultiFactor)} is True.",
                    [nameof(DisableTwoFactorApp), nameof(DisableTwoFactorSms), nameof(DisableTwoFactorEmail), nameof(RequireMultiFactor)]));
            }

            return results;
        }
    }
}