namespace FoxIDs.Models
{
    /// <summary>
    /// OpenID Connect authentication method.
    /// </summary>
    public class OidcUpParty : OidcUpParty<OidcUpClient> { }

    /// <summary>
    /// OpenID Connect authentication method.
    /// </summary>
    public class OidcUpParty<TClient> : OAuthUpParty<TClient> where TClient : OidcUpClient
    {
        public OidcUpParty()
        {
            Type = PartyTypes.Oidc;
        }
    }
}
