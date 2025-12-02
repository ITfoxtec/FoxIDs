using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class PasswordPolicyViewModel
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
        [Display(Name = "Password max length")]
        public int PasswordLength { get; set; }

        [Range(Constants.Models.Track.PasswordLengthMin, Constants.Models.Track.PasswordLengthMax)]
        [Display(Name = "Password max length")]
        public int PasswordMaxLength { get; set; }

        [Display(Name = "Check password complexity")]
        public bool CheckPasswordComplexity { get; set; } = true;

        [Display(Name = "Check password risk based on global password breaches")]
        public bool CheckPasswordRisk { get; set; } = true;

        [MaxLength(Constants.Models.Track.PasswordBannedCharactersLength)]
        [Display(Name = "Banned characters")]
        public string PasswordBannedCharacters { get; set; }

        [Range(Constants.Models.Track.PasswordHistoryMin, Constants.Models.Track.PasswordHistoryMax)]
        [Display(Name = "Password history (number of previous passwords)")]
        public int PasswordHistory { get; set; }

        [Range(Constants.Models.Track.PasswordMaxAgeMin, Constants.Models.Track.PasswordMaxAgeMax)]
        [Display(Name = "Password max age (seconds)")]
        public long PasswordMaxAge { get; set; }

        [Range(Constants.Models.Track.SoftPasswordChangeMin, Constants.Models.Track.SoftPasswordChangeMax)]
        [Display(Name = "Soft password change (seconds)")]
        public long SoftPasswordChange { get; set; }
    }
}
