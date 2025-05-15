using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class UserRequest : IValidatableObject
    {
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        /// <summary>
        /// Add a value to change the users email address. The field is set to an empty string if the value should be removed.
        /// </summary>
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddressEmptyString]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        public string UpdateEmail { get; set; }

        [MaxLength(Constants.Models.User.PhoneLength)]
        [RegularExpression(Constants.Models.User.PhoneRegExPattern)]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        /// <summary>
        /// Add a value to change the users phone number. The field is set to an empty string if the value should be removed.
        /// </summary>
        [MaxLength(Constants.Models.User.PhoneLength)]
        [RegularExpression(Constants.Models.User.PhoneRegExPattern)]
        public string UpdatePhone { get; set; }

        [MaxLength(Constants.Models.User.UsernameLength)]
        [RegularExpression(Constants.Models.User.UsernameRegExPattern)]
        public string Username { get; set; }

        /// <summary>
        /// Add a value to change the users username. The field is set to an empty string if the value should be removed.
        /// </summary>
        [MaxLength(Constants.Models.User.UsernameLength)]
        [RegularExpression(Constants.Models.User.UsernameRegExPattern)]
        public string UpdateUsername { get; set; }

        [Display(Name = "User must confirm account")]
        public bool ConfirmAccount { get; set; }

        [Display(Name = "Email verified")]
        public bool EmailVerified { get; set; }

        [Display(Name = "Phone verified")]
        public bool PhoneVerified { get; set; }

        [Display(Name = "Disable password authentication")]
        public bool? DisablePasswordAuth { get; set; }

        /// <summary>
        /// Passwordless with email require the user to have a email user identifier.
        /// </summary>
        [Display(Name = "Passwordless with email (one-time password)")]
        public bool? EnablePasswordlessEmail { get; set; }

        /// <summary>
        /// Passwordless with SMS require the user to have a phone user identifier.
        /// </summary>
        [Display(Name = "Passwordless with SMS (one-time password)")]
        public bool? EnablePasswordlessSms { get; set; }

        [Display(Name = "User must change password")]
        public bool ChangePassword { get; set; }

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

        [Display(Name = "Two-factor with authenticator app disabled")]
        public bool DisableTwoFactorApp { get; set; }

        [Display(Name = "Two-factor with SMS disabled")]
        public bool DisableTwoFactorSms { get; set; }

        [Display(Name = "Two-factor with email disabled")]
        public bool DisableTwoFactorEmail { get; set; }

        [Display(Name = "Active two-factor authenticator app")]
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

            if (DisablePasswordAuth == true && !(EnablePasswordlessEmail == true || EnablePasswordlessSms == true))
            {
                results.Add(new ValidationResult($"Either enable {nameof(EnablePasswordlessEmail)} or {nameof(EnablePasswordlessSms)} if {nameof(DisablePasswordAuth)} is true.", [nameof(DisablePasswordAuth), nameof(EnablePasswordlessEmail), nameof(EnablePasswordlessSms)]));
            }

            if (EnablePasswordlessEmail == true)
            {
                if (Email.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The field {nameof(Email)} is required to use passwordless with email.", [nameof(Email), nameof(EnablePasswordlessEmail)]));
                }
            }
            if (EnablePasswordlessSms == true)
            {
                if (Phone.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The field {nameof(Phone)} is required to use passwordless with SMS.", [nameof(Phone), nameof(EnablePasswordlessSms)]));
                }
            }

            if (SetPasswordEmail)
            {
                if (Email.IsNullOrEmpty() && UpdateEmail.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"Either the field {nameof(Email)} or the field {nameof(UpdateEmail)} is required to set password with email.", [nameof(Email), nameof(UpdateEmail), nameof(SetPasswordEmail)]));
                }
            }
            if (SetPasswordSms)
            {
                if (Phone.IsNullOrEmpty() && UpdatePhone.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"Either the field {nameof(Phone)} or the field {nameof(UpdatePhone)} is required to set password with SMS.", [nameof(Phone), nameof(UpdatePhone), nameof(SetPasswordSms)]));
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
