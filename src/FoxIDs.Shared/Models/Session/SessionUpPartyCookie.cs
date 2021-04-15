using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace FoxIDs.Models.Session
{
    public class SessionUpPartyCookie : SessionBaseCookie
    {
        [JsonIgnore]
        public override SameSiteMode SameSite { get; set; } = SameSiteMode.None;

        [JsonProperty(PropertyName = "ei")]
        public string ExternalSessionId { get; set; }

        [JsonProperty(PropertyName = "it")]
        public string IdToken { get; set; }
    }
}
