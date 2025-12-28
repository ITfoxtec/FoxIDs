using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace FoxIDs.Logic
{
    public class NemLoginHttpClientFactory : INemLoginHttpClientFactory
    {
        private readonly TimeSpan timeout = TimeSpan.FromSeconds(30);

        public HttpClient CreateClient(X509Certificate2 clientCertificate)
        {
            if (clientCertificate == null) throw new ArgumentNullException(nameof(clientCertificate));

            var handler = new HttpClientHandler { ClientCertificateOptions = ClientCertificateOption.Manual };
            handler.ClientCertificates.Add(clientCertificate);

            var httpClient = new HttpClient(handler, disposeHandler: true)
            {
                Timeout = timeout,
                MaxResponseContentBufferSize = 500000 // 500kB
            };

            return httpClient;
        }
    }
}
