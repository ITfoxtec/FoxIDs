using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class PasswordPolicy : IValidatableObject
    {
        [Required]
        [MaxLength(Constants.Models.Track.PasswordPolicyNameLength)]
        [RegularExpression(Constants.Models.Track.PasswordPolicyNameRegExPattern)]
        [Display(Name = "Password policy group name")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Track.PasswordPolicyDisplayNameLength)]
        [RegularExpression(Constants.Models.Track.DisplayNameRegExPattern)]
        [Display(Name = "Password policy group display name")]
        public string DisplayName { get; set; }

        [Range(Constants.Models.Track.PasswordLengthMin, Constants.Models.Track.PasswordLengthMax)]
        [Display(Name = "Password min length")]
        public int Length { get; set; }

        [Range(Constants.Models.Track.PasswordLengthMin, Constants.Models.Track.PasswordLengthMax)]
        [Display(Name = "Password max length")]
        public int MaxLength { get; set; }

        [Display(Name = "Check password complexity")]
        public bool CheckComplexity { get; set; } = true;

        [Display(Name = "Check password risk based on global password breaches")]
        public bool CheckRisk { get; set; } = true;

        [MaxLength(Constants.Models.Track.PasswordBannedCharactersLength)]
        [Display(Name = "Banned characters")]
        public string BannedCharacters { get; set; }

        [Range(Constants.Models.Track.PasswordHistoryMin, Constants.Models.Track.PasswordHistoryMax)]
        [Display(Name = "Password history")]
        public int History { get; set; }

        [Range(Constants.Models.Track.PasswordMaxAgeMin, Constants.Models.Track.PasswordMaxAgeMax)]
        [Display(Name = "Password max age (seconds)")]
        public long MaxAge { get; set; }

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