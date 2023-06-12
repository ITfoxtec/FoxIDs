using Newtonsoft.Json;

namespace FoxIDs.Models.Sequences
{
    public class SequenceInfo
    {
        [JsonProperty(PropertyName = "c")]
        public string Culture { get; set; }

        [JsonProperty(PropertyName = "d")]
        public string DownPartyId { get; set; }

        [JsonProperty(PropertyName = "y")]
        public PartyTypes DownPartyType { get; set; }

        [JsonProperty(PropertyName = "u")]
        public string UiUpPartyId { get; set; }
    }
}
