using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class TrackLinkUpSequenceData : UpSequenceData, ISequenceKey
    {
        [Required]
        [JsonProperty(PropertyName = "kn")]
        public string KeyName { get; set; }

        [JsonProperty(PropertyName = "kvu")]
        public long KeyValidUntil { get; set; }

        [JsonProperty(PropertyName = "ku")]
        public bool KeyUsed { get; set; }

        [JsonProperty(PropertyName = "a")]
        public IEnumerable<string> Acr { get; set; }
    }
}
