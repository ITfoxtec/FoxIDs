using ITfoxtec.Identity.Models;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ClientKey
    {
        [Required]
        [JsonProperty(PropertyName = "type")]
        public ClientKeyTypes Type { get; set; }

        [Required]
        [JsonProperty(PropertyName = "external_name")]
        public string ExternalName { get; set; }

        [Required]
        [JsonProperty(PropertyName = "public_key")]
        public JsonWebKey PublicKey { get; set; }

        [Required]
        [JsonProperty(PropertyName = "external_id")]
        public string ExternalId { get; set; }
    }
}
