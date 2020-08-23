using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ResourceCultureItem
    {
        [Required]
        [MaxLength(Constants.Models.Resource.CultureLength)]
        [JsonProperty(PropertyName = "culture")]
        public string Culture { get; set; }

        [Required]
        [MaxLength(Constants.Models.Resource.ValueLength)]
        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }
    }
}
