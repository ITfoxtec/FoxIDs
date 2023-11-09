using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class SamlDownSequenceData : ISequenceData, IDownSequenceData
    {
        [Required]
        [JsonProperty(PropertyName = "i")]
        public string Id { get; set; }

        [MaxLength(Constants.Models.SamlParty.RelayStateLength)]
        [JsonProperty(PropertyName = "rs")]
        public string RelayState { get; set; }

        [MaxLength(Constants.Models.SamlParty.AcsResponseUrlLength)]
        [JsonProperty(PropertyName = "a")]
        public string AcsResponseUrl { get; set; }
    }
}
