using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum ClientAuthenticationMethods
    {
        [EnumMember(Value = "client_secret_basic")]
        ClientSecretBasic = 10,
        [EnumMember(Value = "client_secret_post")]
        ClientSecretPost = 20,
        //[EnumMember(Value = "client_secret_jwt")]
        //clientSecretJwt = 30,
        [EnumMember(Value = "private_key_jwt")]
        PrivateKeyJwt = 100,
    }
}
