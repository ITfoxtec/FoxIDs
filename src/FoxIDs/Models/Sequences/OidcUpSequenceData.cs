using FoxIDs.Models.Logic;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class OidcUpSequenceData : UpSequenceData
    { 
        public OidcUpSequenceData() : base() { }

        public OidcUpSequenceData(ILoginRequest loginRequest) : base(loginRequest) { }

        [MaxLength(Constants.Models.OAuthUpParty.Client.ClientIdLength)]
        [JsonProperty(PropertyName = "ci")]
        public string ClientId { get; set; }

        [MaxLength(IdentityConstants.MessageLength.RedirectUriMax)]
        [JsonProperty(PropertyName = "ru")]
        public string RedirectUri { get; set; }

        [MaxLength(IdentityConstants.MessageLength.NonceMax)]
        [JsonProperty(PropertyName = "n")]
        public string Nonce { get; set; }

        [MaxLength(IdentityConstants.MessageLength.CodeVerifierMax)]
        [JsonProperty(PropertyName = "cv")]
        public string CodeVerifier { get; set; }

        [MaxLength(IdentityConstants.MessageLength.SessionIdMax)]
        [JsonProperty(PropertyName = "si")]
        public string SessionId { get; set; }

        [JsonProperty(PropertyName = "lc")]
        public bool RequireLogoutConsent { get; set; }

        [JsonProperty(PropertyName = "lr")]
        public bool PostLogoutRedirect { get; set; }
    }
}
