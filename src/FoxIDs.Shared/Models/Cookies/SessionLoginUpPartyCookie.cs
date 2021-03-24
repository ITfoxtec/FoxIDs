using Newtonsoft.Json;

namespace FoxIDs.Models.Cookies
{
    public class SessionLoginUpPartyCookie : SessionBaseCookie
    {
        [JsonProperty(PropertyName = "e")]
        public string Email { get; set; }
    }
}
