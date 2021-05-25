using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class SamlMetadataRequestedAttribute
    {
        [Required]
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "is_required")]
        public bool IsRequired { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "name_format")]
        public string NameFormat { get; set; }
    }
}
