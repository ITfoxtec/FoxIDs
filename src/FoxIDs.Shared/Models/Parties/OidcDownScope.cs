namespace FoxIDs.Models
{
    public class OidcDownScope : OidcDownScope<OidcDownClaim> { }
    public class OidcDownScope<TClaim> : OAuthDownScope<TClaim> where TClaim : OidcDownClaim
    {
    }
}
