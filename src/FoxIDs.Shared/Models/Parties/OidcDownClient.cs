using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class OidcDownClient : OidcDownClient<OidcDownScope, OidcDownClaim> { }
    public class OidcDownClient<TScope, TClaim> : OAuthDownClient<TScope, TClaim> where TScope : OidcDownScope<TClaim> where TClaim : OidcDownClaim
    {
        [Range(Constants.Models.OidcDownParty.Client.IdTokenLifetimeMin, Constants.Models.OidcDownParty.Client.IdTokenLifetimeMax)]
        [JsonProperty(PropertyName = "id_token_lifetime")]
        public int IdTokenLifetime { get; set; }

        [JsonProperty(PropertyName = "require_logout_id_token_hint")]
        public bool RequireLogoutIdTokenHint { get; set; }
    }
}
