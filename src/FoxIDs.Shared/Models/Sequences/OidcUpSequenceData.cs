using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class OidcUpSequenceData : UpSequenceData
    {
        [MaxLength(500)]
        [JsonProperty(PropertyName = "ci")]
        public string ClientId { get; set; }

        [MaxLength(2000)]
        [JsonProperty(PropertyName = "ru")]
        public string RedirectUri { get; set; }

        [MaxLength(2000)]
        [JsonProperty(PropertyName = "n")]
        public string Nonce { get; set; }

        [MaxLength(500)]
        [JsonProperty(PropertyName = "cv")]
        public string CodeVerifier { get; set; }

        [MaxLength(200)]
        [JsonProperty(PropertyName = "si")]
        public string SessionId { get; set; }

        [JsonProperty(PropertyName = "lc")]
        public bool RequireLogoutConsent { get; set; }

        [JsonProperty(PropertyName = "lr")]
        public bool PostLogoutRedirect { get; set; }
    }
}
