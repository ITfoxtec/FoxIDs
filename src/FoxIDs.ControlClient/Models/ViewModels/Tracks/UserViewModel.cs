using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class UserViewModel : IValidatableObject
    {
        public UserViewModel()
        {
            Claims = new List<ClaimAndValues>();
        }

        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddressEmptyString]
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

        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Passwordless with email (one-time password)")]
        public bool PasswordlessEmail { get; set; }

        [Display(Name = "Passwordless with SMS (one-time password)")]
        public bool PasswordlessSms { get; set; }

        [Display(Name = "Require password change")]
        public bool ChangePassword { get; set; }

        [Display(Name = "Require set password with email confirmation")]
        public bool SetPasswordEmail { get; set; }

        [Display(Name = "Require set password with phone confirmation")]
        public bool SetPasswordSms { get; set; }

        [Display(Name = "Account status")]
        public bool DisableAccount { get; set; }

        [Display(Name = "Two-factor with authenticator App supported")]
        public bool DisableTwoFactorApp { get; set; }

        [Display(Name = "Two-factor with SMS supported")]
        public bool DisableTwoFactorSms { get; set; }

        [Display(Name = "Two-factor with email supported")]
        public bool DisableTwoFactorEmail { get; set; }

        [Display(Name = "Active two-factor authenticator app (only for deactivation)")]
        public bool ActiveTwoFactorApp { get; set; }

        [Display(Name = "Require multi-factor (2FA/MFA)")]
        public bool RequireMultiFactor { get; set; }

        [MaxLength(Constants.Models.User.UserIdLength)]
        [Display(Name = "User id (unique and persistent)")]
        public string UserId { get; set; }

        [ValidateComplexType]
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

            var requerePassword = !(PasswordlessEmail || PasswordlessSms || SetPasswordEmail || SetPasswordSms);
            if (requerePassword)
            {
                if (Password.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field {nameof(Password)} is required.", [nameof(Password)]));
                }
            }

            if (PasswordlessEmail)
            {
                if (Email.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The field {nameof(Email)} is required to use passwordless email.", [nameof(Email), nameof(PasswordlessEmail)]));
                }
            }
            if (PasswordlessSms)
            {
                if (Phone.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The field {nameof(Phone)} is required to use passwordless SMS.", [nameof(Phone), nameof(PasswordlessSms)]));
                }
            }

            if (SetPasswordEmail)
            {
                if (Email.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The field {nameof(Email)} is required to set password with email.", [nameof(Email), nameof(SetPasswordEmail)]));
                }
            }
            if (SetPasswordSms)
            {
                if (Phone.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The field {nameof(Phone)} is required to set password with SMS.", [nameof(Phone), nameof(SetPasswordSms)]));
                }
            }

            if (RequireMultiFactor && DisableTwoFactorApp && DisableTwoFactorSms && DisableTwoFactorEmail)
            {
                results.Add(new ValidationResult($"Either two-factor with authenticator app, SMS or email should be supported if multi-factor (2FA/MFA) is require.",
                    [nameof(DisableTwoFactorApp), nameof(DisableTwoFactorSms), nameof(DisableTwoFactorEmail), nameof(RequireMultiFactor)]));
            }

            return results;
        }
    }
}
