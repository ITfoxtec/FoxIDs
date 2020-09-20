using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class TrackKeyExternalItem
    {
        [Required]
        [JsonProperty(PropertyName = "key")]
        public JsonWebKey Key { get; set; }

        [Required]
        [JsonProperty(PropertyName = "external_id")]
        public string ExternalId { get; set; }
    }
}
