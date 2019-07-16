using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Resources
{
    public class ResourceItem
    {
        [Required]
        [MaxLength(5)]
        [JsonProperty(PropertyName = "culture")]
        public string Culture { get; set; }

        [Required]
        [MaxLength(500)]
        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }
    }
}
