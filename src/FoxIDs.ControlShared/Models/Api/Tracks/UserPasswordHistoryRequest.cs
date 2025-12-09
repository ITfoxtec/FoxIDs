using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Request to set password history for a specific user.
    /// </summary>
    public class UserPasswordHistoryRequest : IValidatableObject
    {
        /// <summary>
        /// User email.
        /// </summary>
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        /// <summary>
        /// User phone number.
        /// </summary>
        [MaxLength(Constants.Models.User.PhoneLength)]
        [RegularExpression(Constants.Models.User.PhoneRegExPattern)]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        /// <summary>
        /// Username.
        /// </summary>
        [MaxLength(Constants.Models.User.UsernameLength)]
        [RegularExpression(Constants.Models.User.UsernameRegExPattern)]
        [Display(Name = "Username")]
        public string Username { get; set; }

        /// <summary>
        /// Password history entries to apply.
        /// </summary>
        [ListLength(Constants.Models.Track.PasswordHistoryMin, Constants.Models.Track.PasswordHistoryMax)]
        [Display(Name = "Password history")]
        public List<PasswordHistoryItem> PasswordHistory { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Email.IsNullOrEmpty() && Phone.IsNullOrEmpty() && Username.IsNullOrEmpty())
            {
                yield return new ValidationResult($"Either the field {nameof(Email)} or the field {nameof(Phone)} or the field {nameof(Username)} is required.", [nameof(Email), nameof(Phone), nameof(Username)]);
            }

            if (PasswordHistory?.Count > 0)
            {
                var duplicateHash = PasswordHistory.GroupBy(ph => new { ph.HashAlgorithm, ph.Hash, ph.HashSalt }).FirstOrDefault(g => g.Count() > 1)?.Key;
                if (duplicateHash != null)
                {
                    yield return new ValidationResult($"Duplicate password history. Hash '{duplicateHash.Hash}'.", [nameof(PasswordHistory)]);
                }
            }
        }
    }
}
