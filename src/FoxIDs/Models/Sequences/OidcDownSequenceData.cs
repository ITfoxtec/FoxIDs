using FoxIDs.Models.Logic;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class OidcDownSequenceData : DownSequenceData, ISequenceData
    {
        public OidcDownSequenceData() : base() { }

        public OidcDownSequenceData(ILoginRequest loginRequest) : base(loginRequest) { }

        [MaxLength(IdentityConstants.MessageLength.ResponseTypeMax)]
        [JsonProperty(PropertyName = "rt")]
        public string ResponseType { get; set; }

        [JsonProperty(PropertyName = "fa")]
        public bool RestrictFormAction { get; set; }        

        [MaxLength(IdentityConstants.MessageLength.RedirectUriMax)]
        [JsonProperty(PropertyName = "ru")]
        public string RedirectUri { get; set; }

        [MaxLength(IdentityConstants.MessageLength.ScopeMax)]
        [JsonProperty(PropertyName = "sc")]
        public string Scope { get; set; }

        [MaxLength(IdentityConstants.MessageLength.StateMax)]
        [JsonProperty(PropertyName = "st")]
        public string State { get; set; }

        [MaxLength(IdentityConstants.MessageLength.ResponseModeMax)]
        [JsonProperty(PropertyName = "rm")]
        public string ResponseMode { get; set; }

        [MaxLength(IdentityConstants.MessageLength.NonceMax)]
        [JsonProperty(PropertyName = "n")]
        public string Nonce { get; set; }

        [MaxLength(IdentityConstants.MessageLength.CodeChallengeMax)]
        [JsonProperty(PropertyName = "cc")]
        public string CodeChallenge { get; set; }

        [MaxLength(IdentityConstants.MessageLength.CodeChallengeMethodMax)]
        [JsonProperty(PropertyName = "cm")]
        public string CodeChallengeMethod { get; set; }

        [MaxLength(Constants.Models.OAuthDownParty.RouteUrlLength)]
        [JsonProperty(PropertyName = "rou")]
        public string RouteUrl { get; set; }
    }
}
