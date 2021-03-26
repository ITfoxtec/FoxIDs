using FoxIDs.Models.Logic;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class UpSequenceData : ISequenceData
    {
        [Required]
        [JsonProperty(PropertyName = "di")]
        public string DownPartyId { get; set; }

        [Required]
        [JsonProperty(PropertyName = "dt")]
        public PartyTypes DownPartyType { get; set; }

        [Required]
        [JsonProperty(PropertyName = "ui")]
        public string UpPartyId { get; set; }

        [JsonProperty(PropertyName = "la")]
        public LoginAction LoginAction { get; set; }

        [JsonProperty(PropertyName = "i")]
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "ma")]
        public int? MaxAge { get; set; }
    }
}
