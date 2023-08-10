namespace FoxIDs.Models.Api
{
    public enum ClientAuthenticationMethods
    {
        ClientSecretBasic = 10,
        ClientSecretPost = 20,
        //clientSecretJwt = 30,
        PrivateKeyJwt = 100,
    }
}
