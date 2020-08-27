using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ResourceName
    {
        [Required]
        [MaxLength(Constants.Models.Resource.NameLength)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [Required]
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }
    }
}
