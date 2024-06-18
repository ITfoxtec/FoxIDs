using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class HelpersService : BaseService
    {
        private const string readCertificateApiUri = "pi/{tenant}/{track}/!readcertificate";
        private const string downPartyTestApiUri = "api/{tenant}/{track}/!downpartytest";

        public HelpersService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<JwkWithCertificateInfo> ReadCertificateAsync(CertificateAndPassword certificateAndPassword) => await PostResponseAsync<CertificateAndPassword, JwkWithCertificateInfo>(readCertificateApiUri, certificateAndPassword);

        public async Task<DownPartyTestStartResponse> StartDownPartyTestAsync(DownPartyTestStartRequest downPartyTestStartRequest) => await PostResponseAsync<DownPartyTestStartRequest, DownPartyTestStartResponse>(downPartyTestApiUri, downPartyTestStartRequest);
    }
}
