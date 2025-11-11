using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class TrackResourceLargeCultureItem
    {
        [Required]
        [MaxLength(Constants.Models.Resource.CultureLength)]
        [JsonProperty(PropertyName = "culture")]
        public string Culture { get; set; }

        [MaxLength(Constants.Models.Resource.ResourceLarge.ValueLength)]
        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }
    }
}
