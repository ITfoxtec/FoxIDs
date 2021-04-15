using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace FoxIDs.Models.Session
{
    public class SessionLoginUpPartyCookie : SessionBaseCookie
    {
        [JsonIgnore]
        public override SameSiteMode SameSite { get; set; } = SameSiteMode.Lax;
    }
}
