using FoxIDs.Models.Session;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace FoxIDs.Models.Sequences
{
    public class StateUpPartyCookie : CookieMessage
    {
        [JsonIgnore]
        public override SameSiteMode SameSite => SameSiteMode.None;

        [JsonProperty(PropertyName = "s")]
        public string State { get; set; }
    }
}
