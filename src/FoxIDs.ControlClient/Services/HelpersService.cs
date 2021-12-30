using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class HelpersService : BaseService
    {
        private const string readcertificateApiUri = "api/{tenant}/{track}/!readcertificate";

        public HelpersService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<JwtWithCertificateInfo> ReadCertificateAsync(CertificateAndPassword certificateAndPassword) => await PostResponseAsync<CertificateAndPassword, JwtWithCertificateInfo>(readcertificateApiUri, certificateAndPassword);
    }
}
