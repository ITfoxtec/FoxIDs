using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class RefreshTokenTtlGrant : RefreshTokenGrant, IDataTtlDocument
    {
        [Required]
        [JsonProperty(PropertyName = "ttl")]
        public int TimeToLive { get; set; }
    }
}
