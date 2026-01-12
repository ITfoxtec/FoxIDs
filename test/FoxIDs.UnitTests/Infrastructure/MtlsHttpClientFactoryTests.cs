using FoxIDs.Infrastructure.HttpClientFactory;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace FoxIDs.UnitTests.Infrastructure
{
    public class MtlsHttpClientFactoryTests
    {
        [Fact]
        public void CreateClient_WithCertificate_UsesConfiguredTimeouts()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

            var factory = new MtlsHttpClientFactory();

            using var client = factory.CreateClient(certificate);

            Assert.Equal(TimeSpan.FromSeconds(30), client.Timeout);
            Assert.Equal(500000, client.MaxResponseContentBufferSize);
        }

        [Fact]
        public void CreateClient_NullCertificate_ThrowsArgumentNullException()
        {
            var factory = new MtlsHttpClientFactory();

            Assert.Throws<ArgumentNullException>(() => factory.CreateClient(null));
        }
    }
}
