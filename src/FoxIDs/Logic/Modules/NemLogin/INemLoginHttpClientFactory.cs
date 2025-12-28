using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace FoxIDs.Logic
{
    public interface INemLoginHttpClientFactory
    {
        HttpClient CreateClient(X509Certificate2 clientCertificate);
    }
}
