using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace FoxIDs
{
    /// <summary>
    /// Extension methods for JsonWebKey.
    /// </summary>
    public static class JsonWebKeyExtensions
    {
        public static Saml2X509Certificate ToSaml2X509Certificate(this JsonWebKey jwk, bool includePrivateParameters = false)
        {
            var certificate = jwk.ToX509Certificate();
            var rsa = jwk.ToRsa(includePrivateParameters);

            return new Saml2X509Certificate(certificate, rsa);
        }
    }
}
