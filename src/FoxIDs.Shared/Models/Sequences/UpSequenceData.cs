using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class UpSequenceData : ISequenceData
    {
        [Required]
        [JsonProperty(PropertyName = "dp")]
        public string DownPartyId { get; set; }

        [Required]
        [JsonProperty(PropertyName = "dt")]
        public PartyType DownPartyType { get; set; }
    }
}
