using Newtonsoft.Json;

namespace FoxIDs.Models
{
    public enum PartyType
    {
        [JsonProperty(PropertyName = "login")]
        Login,
        [JsonProperty(PropertyName = "oauth2")]
        OAuth2,
        [JsonProperty(PropertyName = "oidc")]
        Oidc,
        [JsonProperty(PropertyName = "saml2")]
        Saml2
    }
}
