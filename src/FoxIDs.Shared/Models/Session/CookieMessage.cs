using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;

namespace FoxIDs.Models.Session
{
    public abstract class CookieMessage
    {
        [JsonProperty(PropertyName = "ct")]
        public long CreateTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        [JsonIgnore]
        public abstract SameSiteMode SameSite { get; }
    }
}
