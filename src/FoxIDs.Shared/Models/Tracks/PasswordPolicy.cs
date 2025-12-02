using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class PasswordPolicy : IValidatableObject
    {
        [Required]
        [MaxLength(Constants.Models.Track.PasswordPolicyNameLength)]
        [RegularExpression(Constants.Models.Track.PasswordPolicyNameRegExPattern)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Track.PasswordPolicyDisplayNameLength)]
        [RegularExpression(Constants.Models.Track.DisplayNameRegExPattern)]
        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; set; }

        [Range(Constants.Models.Track.PasswordLengthMin, Constants.Models.Track.PasswordLengthMax)]
        [JsonProperty(PropertyName = "min_length")]
        public int MinLength { get; set; }

        [Range(Constants.Models.Track.PasswordLengthMin, Constants.Models.Track.PasswordLengthMax)]
        [JsonProperty(PropertyName = "max_length")]
        public int MaxLength { get; set; }

        [JsonProperty(PropertyName = "check_complexity")]
        public bool CheckComplexity { get; set; } = true;

        [JsonProperty(PropertyName = "check_risk")]
        public bool CheckRisk { get; set; } = true;

        [MaxLength(Constants.Models.Track.PasswordBannedCharactersLength)]
        [JsonProperty(PropertyName = "banned_characters")]
        public string BannedCharacters { get; set; }

        [Range(Constants.Models.Track.PasswordHistoryMin, Constants.Models.Track.PasswordHistoryMax)]
        [JsonProperty(PropertyName = "history")]
        public int History { get; set; }

        [Range(Constants.Models.Track.PasswordMaxAgeMin, Constants.Models.Track.PasswordMaxAgeMax)]
        [JsonProperty(PropertyName = "max_age")]
        public long MaxAge { get; set; }

        [Range(Constants.Models.Track.SoftPasswordChangeMin, Constants.Models.Track.SoftPasswordChangeMax)]
        [JsonProperty(PropertyName = "soft_change")]
        public long SoftChange { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MaxLength < MinLength)
            {
                yield return new ValidationResult($"The field {nameof(MaxLength)} must be greater than or equal to {nameof(MinLength)}.", [nameof(MaxLength), nameof(MinLength)]);
            }
        }
    }
}
