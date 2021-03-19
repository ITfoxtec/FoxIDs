using Newtonsoft.Json;

namespace FoxIDs.Models.Sequences
{
    public class LoginUpSequenceData : UpSequenceData
    {
        [JsonProperty(PropertyName = "si")]
        public string SessionId { get; set; }

        [JsonProperty(PropertyName = "lc")]
        public bool RequireLogoutConsent { get; set; }

        [JsonProperty(PropertyName = "lr")]
        public bool PostLogoutRedirect { get; set; }

        [JsonProperty(PropertyName = "e")]
        public string Email { get; set; }
    }
}
