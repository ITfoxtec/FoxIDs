using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class HelpersService : BaseService
    {
        private const string readCertificateApiUri = "api/{tenant}/{track}/!readcertificate";
        private const string readCertificatePemApiUri = "api/{tenant}/{track}/!readcertificatefrompem";
        private const string downPartyTestApiUri = "api/{tenant}/{track}/!downpartytest";
        private const string planInfoApiUri = "api/{tenant}/{track}/!planinfo";

        public HelpersService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<JwkWithCertificateInfo> ReadCertificateAsync(CertificateAndPassword certificateAndPassword) => await PostResponseAsync<CertificateAndPassword, JwkWithCertificateInfo>(readCertificateApiUri, certificateAndPassword);
        public async Task<JwkWithCertificateInfo> ReadCertificateFromPemAsync(CertificateCrtAndKey certificateCrtAndKey) => await PostResponseAsync<CertificateCrtAndKey, JwkWithCertificateInfo>(readCertificatePemApiUri, certificateCrtAndKey);

        public async Task<DownPartyTestStartResponse> StartDownPartyTestAsync(DownPartyTestStartRequest downPartyTestStartRequest) => await PostResponseAsync<DownPartyTestStartRequest, DownPartyTestStartResponse>(downPartyTestApiUri, downPartyTestStartRequest);

        public async Task<IEnumerable<PlanInfo>> GetPlanInfoAsync() => await GetAsync<IEnumerable<PlanInfo>>(planInfoApiUri);
    }
}
