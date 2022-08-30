using Newtonsoft.Json;

namespace FoxIDs.Models.Session
{
    public class HrdUpPartyCookieData
    {
        [JsonProperty(PropertyName = "n")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "t")]
        public PartyTypes Type { get; set; }
    }
}