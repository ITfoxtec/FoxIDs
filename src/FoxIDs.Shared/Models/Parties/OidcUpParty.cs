namespace FoxIDs.Models
{
    public class OidcUpParty : OidcUpParty<OidcUpClient> { }

    public class OidcUpParty<TClient> : OAuthUpParty<TClient> where TClient : OidcUpClient
    {
        public OidcUpParty()
        {
            Type = PartyTypes.Oidc;
        }
    }
}
