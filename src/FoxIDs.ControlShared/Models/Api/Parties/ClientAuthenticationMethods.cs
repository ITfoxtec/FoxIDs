namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Methods supported for authenticating OAuth/OIDC clients.
    /// </summary>
    public enum ClientAuthenticationMethods
    {
        ClientSecretPost = 0,
        ClientSecretBasic = 10,        
        //clientSecretJwt = 30,
        PrivateKeyJwt = 100,
    }
}
