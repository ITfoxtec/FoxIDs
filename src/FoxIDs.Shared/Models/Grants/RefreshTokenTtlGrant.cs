using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class RefreshTokenTtlGrant : RefreshTokenGrant, IDataTtlDocument
    {
        [Required]
        [JsonProperty(PropertyName = "ttl")]
        public int TimeToLive { get; set; }

        [JsonProperty(PropertyName = "expire_at")]
        public DateTimeOffset ExpireAt { get { return DateTimeOffset.UtcNow; } set { } }
    }
}
