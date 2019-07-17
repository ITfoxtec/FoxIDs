using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace FoxIDs
{
    /// <summary>
    /// Extension methods for X509Certificate2.
    /// </summary>
    public static class X509Certificate2Extensions
    {
        public static Task<X509Certificate2> CreateSelfSignedCertificateAsync(this string cn)
        {
            using (var rsa = RSA.Create(2048))
            {
                var certRequest = new CertificateRequest(
                    $"CN={cn}, O=FoxIDs",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                certRequest.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(false, false, 0, false));

                certRequest.CertificateExtensions.Add(
                    new X509SubjectKeyIdentifierExtension(certRequest.PublicKey, false));

                certRequest.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyAgreement,
                        false));

                var now = DateTimeOffset.UtcNow;
                return Task.FromResult(certRequest.CreateSelfSigned(now.AddDays(-1), now.AddDays(365)));
            }
        }
    }
}
