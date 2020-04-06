using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class OidcDownSequenceData : ISequenceData
    {
        [MaxLength(50)]
        [JsonProperty(PropertyName = "rt")]
        public string ResponseType { get; set; }

        [MaxLength(2000)]
        [JsonProperty(PropertyName = "ru")]
        public string RedirectUri { get; set; }

        [MaxLength(2000)]
        [JsonProperty(PropertyName = "sc")]
        public string Scope { get; set; }

        [MaxLength(2000)]
        [JsonProperty(PropertyName = "st")]
        public string State { get; set; }

        [MaxLength(50)]
        [JsonProperty(PropertyName = "rm")]
        public string ResponseMode { get; set; }

        [MaxLength(2000)]
        [JsonProperty(PropertyName = "n")]
        public string Nonce { get; set; }

        [MaxLength(500)]
        [JsonProperty(PropertyName = "cc")]
        public string CodeChallenge { get; set; }

        [MaxLength(10)]
        [JsonProperty(PropertyName = "cm")]
        public string CodeChallengeMethod { get; set; }
    }
}
