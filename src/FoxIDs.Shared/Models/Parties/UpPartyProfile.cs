using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public abstract class UpPartyProfile
    {
        [Required]
        [MaxLength(Constants.Models.Party.ProfileNameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; set; }
    }
}
