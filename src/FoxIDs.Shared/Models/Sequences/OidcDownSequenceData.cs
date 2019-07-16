using Newtonsoft.Json;

namespace FoxIDs.Models.Sequences
{
    public class OidcDownSequenceData : ISequenceData
    {
        [JsonProperty(PropertyName = "rt")]
        public string ResponseType { get; set; }

        [JsonProperty(PropertyName = "ru")]
        public string RedirectUri { get; set; }

        [JsonProperty(PropertyName = "sc")]
        public string Scope { get; set; }

        [JsonProperty(PropertyName = "st")]
        public string State { get; set; }

        [JsonProperty(PropertyName = "rm")]
        public string ResponseMode { get; set; }

        [JsonProperty(PropertyName = "n")]
        public string Nonce { get; set; }
    }
}
