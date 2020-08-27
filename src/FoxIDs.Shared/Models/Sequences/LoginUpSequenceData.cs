using FoxIDs.Models.Logic;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class LoginUpSequenceData : UpSequenceData
    {
        [Required]
        [JsonProperty(PropertyName = "up")]
        public string UpPartyId { get; set; }

        [JsonProperty(PropertyName = "si")]
        public string SessionId { get; set; }

        [JsonProperty(PropertyName = "lc")]
        public bool RequireLogoutConsent { get; set; }

        [JsonProperty(PropertyName = "lr")]
        public bool PostLogoutRedirect { get; set; }

        [JsonProperty(PropertyName = "la")]
        public LoginAction LoginAction { get; set; }

        [JsonProperty(PropertyName = "i")]
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "ma")]
        public int? MaxAge { get; set; }

        [JsonProperty(PropertyName = "eh")]
        public string EmailHint { get; set; }

        [JsonProperty(PropertyName = "c")]
        public string Culture { get; set; }
    }
}
