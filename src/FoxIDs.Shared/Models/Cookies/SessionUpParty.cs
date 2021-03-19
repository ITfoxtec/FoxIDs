using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models.Cookies
{
    public class SessionUpParty : CookieMessage
    {
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
        public List<ClaimAndValues> Claims { get; set; }

        [JsonProperty(PropertyName = "it")]
        public string IdToken { get; set; }
    }
}
