using ITfoxtec.Identity;
using ITfoxtec.Identity.Models;
using ITfoxtec.Identity.Saml2.Cryptography;
using MTokens = Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using ITfoxtec.Identity.Saml2;
using static ITfoxtec.Identity.IdentityConstants;

namespace FoxIDs
{
    /// <summary>
    /// Extension methods for JsonWebKey.
    /// </summary>
    public static class JsonWebKeyExtensions
    {
        public static (IEnumerable<JsonWebKey> validKeys, IEnumerable<(JsonWebKey key, X509Certificate2 certificate)> invalidKeys) GetValidKeys(this IEnumerable<JsonWebKey> keys)
        {
            var validKeys = new List<JsonWebKey>();
            var invalidKeys = new List<(JsonWebKey key, X509Certificate2 certificate)>();
            if (keys?.Count() > 0)
            {
                foreach (var key in keys)
                {
                    (var isValid, var certificate) = key.IsValid();
                    if (isValid)
                    {
                        validKeys.Add(key);
                    }
                    else
                    {
                        invalidKeys.Add((key, certificate));
                    }
                }
            }
            return (validKeys, invalidKeys);
        }

        public static (bool, X509Certificate2) IsValid(this JsonWebKey key)
        {
            if (key.Kty == MTokens.JsonWebAlgorithmsKeyTypes.RSA && key.X5c?.Count >= 1)
            {
                var certificate = key.ToX509Certificate();
                return (certificate.IsValidLocalTime(), certificate);
            }
            else
            {
                return (true, null);
            }
        }

        public static Saml2X509Certificate ToSaml2X509Certificate(this JsonWebKey jwk, bool includePrivateParameters = false)
        {
            var certificate = jwk.ToX509Certificate();
            var rsa = jwk.ToRsa(includePrivateParameters);

            return new Saml2X509Certificate(certificate, rsa);
        }


        public static IEnumerable<Saml2X509Certificate> ToSaml2X509Certificates(this IEnumerable<JsonWebKey> jwks)
        {
            var certificates = new List<Saml2X509Certificate>();
            if (jwks?.Count() > 0)
            {
                foreach (var jwk in jwks)
                {
                    certificates.Add(jwk.ToSaml2X509Certificate());
                }
            }
            return certificates;
        }

        public static JsonWebKey AddSignatureUse(this JsonWebKey key)
        {
            key.Use = JsonPublicKeyUse.Signature;
            return key;
        }
    }
}
