using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models.Cookies
{
    public class SessionUpPartyCookie : SessionBaseCookie
    {
        [JsonProperty(PropertyName = "ei")]
        public string ExternalSessionId { get; set; }

        [JsonProperty(PropertyName = "c")]
        public IEnumerable<ClaimAndValues> Claims { get; set; }

        [JsonProperty(PropertyName = "it")]
        public string IdToken { get; set; }
    }
}
