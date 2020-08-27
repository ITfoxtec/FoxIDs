using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class TrackKey
    {
        [Required]
        [JsonProperty(PropertyName = "type")]
        public TrackKeyType Type { get; set; }

        [Required]
        [JsonProperty(PropertyName = "key")]
        public JsonWebKey Key { get; set; }

        [JsonProperty(PropertyName = "external_name")]
        public string ExternalName { get; set; }
    }
}
