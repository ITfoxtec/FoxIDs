using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models.Cookies
{
    public abstract class SessionBaseCookie : CookieMessage
    {
        [JsonProperty(PropertyName = "lu")]
        public long LastUpdated { get; set; }

        [JsonProperty(PropertyName = "am")]
        public List<string> AuthMethods { get; set; }

        [JsonProperty(PropertyName = "si")]
        public string SessionId { get; set; }

        [JsonProperty(PropertyName = "ui")]
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "dp")]
        public List<DownPartyLink> DownPartyLinks { get; set; }
    }
}
