using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Models.Session
{
    public abstract class SessionBaseCookie : CookieMessage
    {
        [JsonIgnore]
        public string SessionIdClaim => Claims.FindFirstOrDefaultValue(c => c.Claim == JwtClaimTypes.SessionId);

        [JsonIgnore]
        public string UserIdClaim => Claims.FindFirstOrDefaultValue(c => c.Claim == JwtClaimTypes.Subject);

        [JsonIgnore]
        public string EmailClaim => Claims.FindFirstOrDefaultValue(c => c.Claim == JwtClaimTypes.Email);

        [JsonProperty(PropertyName = "c")]
        public IEnumerable<ClaimAndValues> Claims { get; set; }

        [JsonProperty(PropertyName = "dl")]
        public List<DownPartySessionLink> DownPartyLinks { get; set; }

        [JsonProperty(PropertyName = "m")]
        public bool IsMarkerSession { get; set; }
    }
}
