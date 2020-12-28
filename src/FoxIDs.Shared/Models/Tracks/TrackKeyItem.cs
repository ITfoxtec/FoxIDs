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
    }
}
