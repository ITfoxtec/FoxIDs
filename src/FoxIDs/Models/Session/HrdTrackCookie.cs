using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models.Session
{
    public class HrdTrackCookie : CookieMessage
    {
        [JsonIgnore]
        public override SameSiteMode SameSite => SameSiteMode.None;

        [JsonProperty(PropertyName = "up")]
        public IEnumerable<HrdUpPartyCookieData> UpParties { get; set; }
    }
}
