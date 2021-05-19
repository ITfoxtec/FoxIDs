using ITfoxtec.Identity;
using ITfoxtec.Identity.Models;
using ITfoxtec.Identity.Saml2.Cryptography;
using MTokens = Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

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
                var nowLocal = DateTime.Now;
                foreach (var key in keys)
                {
                    (var isValid, var certificate) = key.IsValid(nowLocal);
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

        public static (bool, X509Certificate2) IsValid(this JsonWebKey key, DateTime nowLocal)
        {
            if (key.Kty == MTokens.JsonWebAlgorithmsKeyTypes.RSA && key.X5c?.Count >= 1)
            {
                var certificate = key.ToX509Certificate();
                return (certificate.IsValid(nowLocal), certificate);
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
    }
}
