using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models.Cookies
{
    public class SessionCookie : CookieMessage
    {
        [JsonProperty(PropertyName = "lu")]
        public long LastUpdated { get; set; }

        [JsonProperty(PropertyName = "am")]
        public List<string> AuthMethods { get; set; }

        [JsonProperty(PropertyName = "si")]
        public string SessionId { get; set; }

        [JsonProperty(PropertyName = "e")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "ui")]
        public string UserId { get; set; }
    }
}
