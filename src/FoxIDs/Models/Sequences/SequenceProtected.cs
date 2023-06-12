using Newtonsoft.Json;

namespace FoxIDs.Models.Sequences
{
    public class SequenceProtected
    {
        [JsonProperty(PropertyName = "i")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "t")]
        public long CreateTime { get; set; }

        [JsonProperty(PropertyName = "a")]
        public bool? AccountAction { get; set; }
    }
}
