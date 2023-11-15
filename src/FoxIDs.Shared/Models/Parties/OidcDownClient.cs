using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class OidcDownClient : OidcDownClient<OidcDownScope, OidcDownClaim> { }
    public class OidcDownClient<TScope, TClaim> : OAuthDownClient<TScope, TClaim> where TScope : OidcDownScope<TClaim> where TClaim : OidcDownClaim
    {
        [ListLength(Constants.Models.OidcDownParty.Client.ResponseTypesMin, Constants.Models.OAuthDownParty.Client.ResponseTypesMax, Constants.Models.OAuthDownParty.Client.ResponseTypeLength)]
        [JsonProperty(PropertyName = "response_types")]
        public override List<string> ResponseTypes { get; set; }

        [ListLength(Constants.Models.OidcDownParty.Client.RedirectUrisMin, Constants.Models.OAuthDownParty.Client.RedirectUrisMax, Constants.Models.OAuthDownParty.Client.RedirectUriLength)]
        [JsonProperty(PropertyName = "redirect_uris")]
        public override List<string> RedirectUris { get; set; }

        [Range(Constants.Models.OidcDownParty.Client.IdTokenLifetimeMin, Constants.Models.OidcDownParty.Client.IdTokenLifetimeMax)]
        [JsonProperty(PropertyName = "id_token_lifetime")]
        public int IdTokenLifetime { get; set; } = 300;

        [JsonProperty(PropertyName = "require_logout_id_token_hint")]
        public bool RequireLogoutIdTokenHint { get; set; }

        [MaxLength(Constants.Models.OAuthUpParty.Client.ResponseModeLength)]
        [JsonProperty(PropertyName = "response_mode")]
        public string ResponseMode { get; set; } = IdentityConstants.ResponseModes.Query;

        [MaxLength(Constants.Models.OAuthDownParty.Client.RedirectUriLength)]
        [JsonProperty(PropertyName = "post_logout_redirect_uri")]
        public string PostLogoutRedirectUri { get; set; }

        [MaxLength(Constants.Models.OAuthDownParty.Client.RedirectUriLength)]
        [JsonProperty(PropertyName = "frontchannel_logout_uri")]
        public string FrontChannelLogoutUri { get; set; }

        [JsonProperty(PropertyName = "frontchannel_logout_session_required")]
        public bool FrontChannelLogoutSessionRequired { get; set; } = true;
    }
}
