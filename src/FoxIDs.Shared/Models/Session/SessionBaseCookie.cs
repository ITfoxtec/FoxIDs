using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Models.Session
{
    public abstract class SessionBaseCookie : CookieMessage
    {
        [JsonProperty(PropertyName = "lu")]
        public long LastUpdated { get; set; }

        [JsonIgnore]
        public IEnumerable<string> AuthMethods => Claims?.Where(c => c.Claim == JwtClaimTypes.Amr)?.SelectMany(c => c.Values);

        [JsonIgnore]
        public string SessionId => Claims.FindFirstValue(c => c.Claim == JwtClaimTypes.SessionId);

        [JsonIgnore]
        public string UserId => Claims.FindFirstValue(c => c.Claim == JwtClaimTypes.Subject);

        [JsonIgnore]
        public string Email => Claims.FindFirstValue(c => c.Claim == JwtClaimTypes.Email);

        [JsonProperty(PropertyName = "c")]
        public IEnumerable<ClaimAndValues> Claims { get; set; }

        [JsonProperty(PropertyName = "dl")]
        public List<DownPartySessionLink> DownPartyLinks { get; set; }
    }
}
