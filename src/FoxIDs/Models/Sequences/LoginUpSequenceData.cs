using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models.Sequences
{
    public class LoginUpSequenceData : UpSequenceData, ILoginUpSequenceDataBase
    {
        [JsonProperty(PropertyName = "si")]
        public string SessionId { get; set; }

        [JsonProperty(PropertyName = "lc")]
        public bool RequireLogoutConsent { get; set; }

        [JsonProperty(PropertyName = "lr")]
        public bool PostLogoutRedirect { get; set; }

        [JsonProperty(PropertyName = "srl")]
        public bool DoSessionUserRequireLogin { get; set; }

        [JsonProperty(PropertyName = "uin")]
        public string UserIdentifier { get; set; }

        [JsonProperty(PropertyName = "e")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "ev")]
        public bool EmailVerified { get; set; }

        [JsonProperty(PropertyName = "p")]
        public string Phone { get; set; }

        [JsonProperty(PropertyName = "pv")]
        public bool PhoneVerified { get; set; }

        [JsonProperty(PropertyName = "tui")]
        public IEnumerable<HrdUpPartySequenceData> ToUpParties { get; set; }

        [JsonProperty(PropertyName = "li")]
        public bool DoLoginIdentifierStep { get; set; }

        [JsonProperty(PropertyName = "a")]
        public IEnumerable<string> Acr { get; set; }

        [JsonProperty(PropertyName = "am")]
        public IEnumerable<string> AuthMethods { get; set; }

        [JsonProperty(PropertyName = "fst")]
        public TwoFactorAppSequenceStates TwoFactorAppState { get; set; }

        [JsonProperty(PropertyName = "fas")]
        public string TwoFactorAppSecret { get; set; }

        [JsonProperty(PropertyName = "fns")]
        public string TwoFactorAppNewSecret { get; set; }

        [JsonProperty(PropertyName = "frc")]
        public string TwoFactorAppRecoveryCode { get; set; }
    }
}
