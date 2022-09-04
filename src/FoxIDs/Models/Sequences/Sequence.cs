using Newtonsoft.Json;

namespace FoxIDs.Models.Sequences
{
    public class Sequence
    {
        [JsonProperty(PropertyName = "i")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "t")]
        public long CreateTime { get; set; }

        [JsonProperty(PropertyName = "c")]
        public string Culture { get; set; }

        [JsonProperty(PropertyName = "a")]
        public bool? AccountAction { get; set; }

        [JsonProperty(PropertyName = "u")]
        public string UiUpPartyId { get; set; }
    }
}
