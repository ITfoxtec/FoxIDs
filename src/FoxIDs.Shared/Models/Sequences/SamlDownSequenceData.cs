using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class SamlDownSequenceData : ISequenceData
    {
        [Required]
        [JsonProperty(PropertyName = "i")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "rs")]
        public string RelayState { get; set; }

        [JsonProperty(PropertyName = "a")]
        public string ResponseUrl { get; set; }
    }
}
