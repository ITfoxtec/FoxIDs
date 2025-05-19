using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class UserRequest : UserBase, IValidatableObject
    {
        /// <summary>
        /// Add a value to change the users email address. The field is set to an empty string if the value should be removed.
        /// </summary>
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddressEmptyString]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        public string UpdateEmail { get; set; }

        /// <summary>
        /// Add a value to change the users phone number. The field is set to an empty string if the value should be removed.
        /// </summary>
        [MaxLength(Constants.Models.User.PhoneLength)]
        [RegularExpression(Constants.Models.User.PhoneRegExPattern)]
        public string UpdatePhone { get; set; }

        /// <summary>
        /// Add a value to change the users username. The field is set to an empty string if the value should be removed.
        /// </summary>
        [MaxLength(Constants.Models.User.UsernameLength)]
        [RegularExpression(Constants.Models.User.UsernameRegExPattern)]
        public string UpdateUsername { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Email.IsNullOrEmpty() && Phone.IsNullOrEmpty() && Username.IsNullOrEmpty())
            {
                results.Add(new ValidationResult($"Either the field {nameof(Email)} or the field {nameof(Phone)} or the field {nameof(Username)} is required.", [nameof(Email), nameof(Phone), nameof(Username)]));
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
