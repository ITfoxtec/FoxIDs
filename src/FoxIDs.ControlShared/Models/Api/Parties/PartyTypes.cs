namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Supported party types within a track.
    /// </summary>
    public enum PartyTypes
    {
        Login = 10,
        OAuth2 = 20,
        Oidc = 30,
        Saml2 = 40,
        TrackLink = 100,
        ExternalLogin = 200
    }
}
