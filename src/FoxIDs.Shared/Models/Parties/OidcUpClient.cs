using Newtonsoft.Json;

namespace FoxIDs.Models
{
    public class OidcUpClient : OAuthUpClient
    {
        [JsonProperty(PropertyName = "use_id_token_claims")]
        public bool UseIdTokenClaims { get; set; }
    }
}
