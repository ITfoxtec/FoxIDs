﻿using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Models.Session
{
    public abstract class SessionBaseCookie : CookieMessage
    {
        [JsonIgnore]
        public IEnumerable<string> AuthMethods => Claims?.Where(c => c.Claim == JwtClaimTypes.Amr)?.SelectMany(c => c.Values);

        [JsonIgnore]
        public string SessionId => Claims.FindFirstOrDefaultValue(c => c.Claim == JwtClaimTypes.SessionId);

        [JsonIgnore]
        public string UserId => Claims.FindFirstOrDefaultValue(c => c.Claim == JwtClaimTypes.Subject);

        [JsonIgnore]
        public string Email => Claims.FindFirstOrDefaultValue(c => c.Claim == JwtClaimTypes.Email);

        [JsonProperty(PropertyName = "c")]
        public IEnumerable<ClaimAndValues> Claims { get; set; }

        [JsonProperty(PropertyName = "dl")]
        public List<DownPartySessionLink> DownPartyLinks { get; set; }
    }
}
