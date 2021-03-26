using Newtonsoft.Json;

namespace FoxIDs.Models.Cookies
{
    public class SessionUpPartyCookie : SessionBaseCookie
    {
        [JsonProperty(PropertyName = "ei")]
        public string ExternalSessionId { get; set; }

        [JsonProperty(PropertyName = "it")]
        public string IdToken { get; set; }
    }
}
