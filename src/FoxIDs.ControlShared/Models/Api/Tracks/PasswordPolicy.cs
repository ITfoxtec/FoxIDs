using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Password policy settings applied to users in a track.
    /// </summary>
    public class PasswordPolicy : IValidatableObject
    {
        /// <summary>
        /// Technical password policy group name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Track.PasswordPolicyNameLength)]
        [RegularExpression(Constants.Models.Track.PasswordPolicyNameRegExPattern)]
        [Display(Name = "Password policy group name")]
        public string Name { get; set; }

        /// <summary>
        /// Display name shown in the UI.
        /// </summary>
        [MaxLength(Constants.Models.Track.PasswordPolicyDisplayNameLength)]
        [RegularExpression(Constants.Models.Track.DisplayNameRegExPattern)]
        [Display(Name = "Password policy group display name")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Minimum required password length.
        /// </summary>
        [Range(Constants.Models.Track.PasswordLengthMin, Constants.Models.Track.PasswordLengthMax)]
        [Display(Name = "Password min length")]
        public int Length { get; set; }

        /// <summary>
        /// Maximum allowed password length.
        /// </summary>
        [Range(Constants.Models.Track.PasswordLengthMin, Constants.Models.Track.PasswordLengthMax)]
        [Display(Name = "Password max length")]
        public int MaxLength { get; set; }

        /// <summary>
        /// Validate passwords for complexity requirements.
        /// </summary>
        [Display(Name = "Check password complexity")]
        public bool CheckComplexity { get; set; } = true;

        /// <summary>
        /// Validate passwords against known breach lists.
        /// </summary>
        [Display(Name = "Check password risk based on global password breaches")]
        public bool CheckRisk { get; set; } = true;

        /// <summary>
        /// Characters not allowed in passwords.
        /// </summary>
        [MaxLength(Constants.Models.Track.PasswordBannedCharactersLength)]
        [Display(Name = "Banned characters (case-insensitive)")]
        public string BannedCharacters { get; set; }

        /// <summary>
        /// Number of previous passwords to track and block.
        /// </summary>
        [Range(Constants.Models.Track.PasswordHistoryMin, Constants.Models.Track.PasswordHistoryMax)]
        [Display(Name = "Password history")]
        public int History { get; set; }

        /// <summary>
        /// Maximum age of a password in seconds.
        /// </summary>
        [Range(Constants.Models.Track.PasswordMaxAgeMin, Constants.Models.Track.PasswordMaxAgeMax)]
        [Display(Name = "Password max age (seconds)")]
        public long MaxAge { get; set; }

        /// <summary>
        /// Soft change period before enforcing a password update.
        /// </summary>
        [Range(Constants.Models.Track.SoftPasswordChangeMin, Constants.Models.Track.SoftPasswordChangeMax)]
        [Display(Name = "Soft password change (seconds)")]
        public long SoftChange { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MaxLength < Length)
            {
                yield return new ValidationResult($"The field {nameof(MaxLength)} must be greater than or equal to {nameof(Length)}.", [nameof(MaxLength), nameof(Length)]);
            }
        }
    }
}
