namespace FoxIDs.Models
{
    public class OidcDownParty : OidcDownParty<OidcDownClient, OidcDownScope, OidcDownClaim> { }
    public class OidcDownParty<TClient, TScope, TClaim> : OAuthDownParty<TClient, TScope, TClaim> where TClient : OidcDownClient<TScope, TClaim> where TScope : OidcDownScope<TClaim> where TClaim : OidcDownClaim
    {
        public OidcDownParty()
        {
            Type = PartyTypes.Oidc;
        }
    }
}
