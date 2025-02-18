using FoxIDs.Models.Logic;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class SamlDownSequenceData : DownSequenceData, ISequenceData
    { 
        public SamlDownSequenceData() : base() { }

        public SamlDownSequenceData(ILoginRequest loginRequest) : base(loginRequest) { }

        [Required]
        [JsonProperty(PropertyName = "i")]
        public string Id { get; set; }

        [MaxLength(Constants.Models.SamlParty.RelayStateLength)]
        [JsonProperty(PropertyName = "rs")]
        public string RelayState { get; set; }

        [MaxLength(Constants.Models.SamlParty.AcsResponseUrlLength)]
        [JsonProperty(PropertyName = "au")]
        public string AcsResponseUrl { get; set; }
    }
}
