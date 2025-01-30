using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models.Session
{
    public class SessionTrackCookie : CookieMessage
    {
        [JsonIgnore]
        public override SameSiteMode SameSite => SameSiteMode.None;

        [JsonProperty(PropertyName = "ul")]
        public List<UpPartySessionLink> UpPartyLinks { get; set; }  
        
        [JsonProperty(PropertyName = "dl")]
        public List<DownPartySessionLink> DownPartyLinks { get; set; }

        [JsonProperty(PropertyName = "c")]
        public IEnumerable<ClaimAndValues> Claims { get; set; }
    }
}
