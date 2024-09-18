using Newtonsoft.Json;

namespace FoxIDs.Models.Session
{
    public class HrdUpPartyCookieData
    {
        [JsonProperty(PropertyName = "ln")]
        public string LoginUpPartyName { get; set; }

        [JsonProperty(PropertyName = "sn")]
        public string SelectedUpPartyName { get; set; }

        [JsonProperty(PropertyName = "spn")]
        public string SelectedUpPartyProfileName { get; set; }

        [JsonProperty(PropertyName = "st")]
        public PartyTypes SelectedUpPartyType { get; set; }
    }
}