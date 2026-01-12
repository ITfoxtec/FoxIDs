using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace FoxIDs.Infrastructure.HttpClientFactory
{
    public interface IMtlsHttpClientFactory
    {
        HttpClient CreateClient(X509Certificate2 clientCertificate);
    }
}
