using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace FoxIDs.Models
{
    public class SessionUpParty
    {
        [JsonProperty(PropertyName = "ct")]
        public long CreateTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        [JsonProperty(PropertyName = "lu")]
        public long LastUpdated { get; set; }

        [JsonProperty(PropertyName = "am")]
        public List<string> AuthMethods { get; set; }

        [JsonProperty(PropertyName = "si")]
        public string SessionId { get; set; }

        [JsonProperty(PropertyName = "esi")]
        public string ExternalSessionId { get; set; }

        [JsonProperty(PropertyName = "ui")]
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "c")]
        public List<Claim> Claims { get; set; }
    }
}
