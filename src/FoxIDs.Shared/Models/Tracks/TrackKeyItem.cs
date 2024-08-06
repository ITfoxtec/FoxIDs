using ITfoxtec.Identity.Models;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class TrackKeyItem
    {
        [Required]
        [JsonProperty(PropertyName = "key")]
        public JsonWebKey Key { get; set; }

        [JsonProperty(PropertyName = "not_before")]
        public long NotBefore { get; set; }

        [JsonProperty(PropertyName = "not_after")]
        public long NotAfter { get; set; }
    }
}
