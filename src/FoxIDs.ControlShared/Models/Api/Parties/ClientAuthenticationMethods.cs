namespace FoxIDs.Models.Api
{
    public enum ClientAuthenticationMethods
    {
        ClientSecretPost = 0,
        ClientSecretBasic = 10,        
        //clientSecretJwt = 30,
        PrivateKeyJwt = 100,
    }
}
