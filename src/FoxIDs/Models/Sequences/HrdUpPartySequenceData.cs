using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class HrdUpPartySequenceData
    {
        [Required]
        [JsonProperty(PropertyName = "n")]
        public string Name { get; set; }

        [Required]
        [JsonProperty(PropertyName = "t")]
        public PartyTypes Type { get; set; }

        [JsonProperty(PropertyName = "hd")]
        public List<string> HrdDomains { get; set; }

        [JsonProperty(PropertyName = "hs")]
        public bool HrdShowButtonWithDomain { get; set; }

        [JsonProperty(PropertyName = "hn")]
        public string HrdDisplayName { get; set; }

        [JsonProperty(PropertyName = "hl")]
        public string HrdLogoUrl { get; set; }
    }
}
