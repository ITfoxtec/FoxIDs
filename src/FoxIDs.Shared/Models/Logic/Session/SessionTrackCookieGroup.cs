using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models.Session
{
    public class SessionTrackCookieGroup
    {
        [JsonProperty(PropertyName = "s")]
        public string SequenceId { get; set; }

        [JsonProperty(PropertyName = "u")]
        public List<UpPartySessionLink> UpPartyLinks { get; set; }

        [JsonProperty(PropertyName = "su")]
        public UpPartySessionLink SessionUpParty { get; set; }

        [JsonProperty(PropertyName = "d")]
        public List<DownPartySessionLink> DownPartyLinks { get; set; }

        [JsonProperty(PropertyName = "c")]
        public IEnumerable<ClaimAndValues> Claims { get; set; }
    }
}
