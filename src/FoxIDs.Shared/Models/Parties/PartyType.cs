using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum PartyType
    {
        [EnumMember(Value = "login")]
        Login = 10,
        [EnumMember(Value = "oauth2")]
        OAuth2 = 20,
        [EnumMember(Value = "oidc")]
        Oidc = 30,
        [EnumMember(Value = "saml2")]
        Saml2 = 40
    }
}
