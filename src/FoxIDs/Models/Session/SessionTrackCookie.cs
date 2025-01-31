using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models.Session
{
    public class SessionTrackCookie : CookieMessage
    {
        [JsonIgnore]
        public override SameSiteMode SameSite => SameSiteMode.None;

        [JsonProperty(PropertyName = "g")]
        public List<SessionTrackCookieGroup> Groups { get; set; }  
    }
}
