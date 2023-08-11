using ITfoxtec.Identity;
using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum ClientAuthenticationMethods
    {
        [EnumMember(Value = IdentityConstants.ClientAuthenticationMethods.ClientSecretBasic)]
        ClientSecretBasic = 10,
        [EnumMember(Value = IdentityConstants.ClientAuthenticationMethods.ClientSecretPost)]
        ClientSecretPost = 20,
        //[EnumMember(Value = IdentityConstants.ClientAuthenticationMethods.clientSecretJwt)]
        //clientSecretJwt = 30,
        [EnumMember(Value = IdentityConstants.ClientAuthenticationMethods.PrivateKeyJwt)]
        PrivateKeyJwt = 100,
    }
}
