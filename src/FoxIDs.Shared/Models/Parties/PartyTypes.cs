using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum PartyTypes
    {
        [EnumMember(Value = "login")]
        Login = 10,
        [EnumMember(Value = "oauth2")]
        OAuth2 = 20,
        [EnumMember(Value = "oidc")]
        Oidc = 30,
        [EnumMember(Value = "saml2")]
        Saml2 = 40,
        [EnumMember(Value = "track_link")]
        TrackLink = 100
    }
}
