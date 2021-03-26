using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class SingleLogoutSequenceData : ISequenceData
    {
        [Required]
        [MaxLength(200)]
        [JsonProperty(PropertyName = "si")]
        public string SessionId { get; set; }

        [Required]
        [JsonProperty(PropertyName = "un")]
        public string UpPartyName { get; set; }

        [Required]
        [JsonProperty(PropertyName = "ut")]
        public PartyTypes UpPartyType { get; set; }

        [Required]
        [JsonProperty(PropertyName = "di")]
        public string DownPartyId { get; set; }

        [Required]
        [JsonProperty(PropertyName = "dt")]
        public PartyTypes DownPartyType { get; set; }

        [JsonProperty(PropertyName = "dl")]
        public List<DownPartyLink> DownPartyLinks { get; set; }
    }
}
