using Newtonsoft.Json;

namespace FoxIDs.Models.Sequences
{
    public class SequenceStep
    {
        public SequenceStep()
        {
            Type = GetType().Name;
        }

        [JsonProperty(PropertyName = "t")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "i")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "c")]
        public long SequenceCreateTime { get; set; }

        [JsonProperty(PropertyName = "l")]
        public int SequenceTimeToLive { get; set; }
    }
}
