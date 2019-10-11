using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace FoxIDs.Models
{
    public class TrackKey
    {
        [JsonProperty(PropertyName = "type")]
        public TrackKeyType Type { get; set; }

        [JsonProperty(PropertyName = "key")]
        public JsonWebKey Key { get; set; }

        [JsonProperty(PropertyName = "external_name")]
        public string ExternalName { get; set; }
    }
}
