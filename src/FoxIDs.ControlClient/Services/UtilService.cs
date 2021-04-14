using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class UtilService : BaseService
    {
        private const string apiUri = "api/{tenant}/master/!convertcertificate";

        public UtilService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<ConvertCertificateResponse> ConvertCertificateAsync(ConvertCertificateRequest certificateRequest) => await PostResponseAsync<ConvertCertificateRequest, ConvertCertificateResponse>(apiUri, certificateRequest);
    }
}
