using Newtonsoft.Json;

namespace FoxIDs.Models
{
    public class OidcDownClaim : OAuthDownClaim
    {
        [JsonProperty(PropertyName = "in_id_token")]
        public bool InIdToken { get; set; }
    }
}
