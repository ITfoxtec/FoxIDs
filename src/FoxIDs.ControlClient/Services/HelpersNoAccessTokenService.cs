using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class HelpersNoAccessTokenService : BaseService
    {
        private const string downPartyTestApiUri = "api/{tenant}/{track}/!downpartytest";

        public HelpersNoAccessTokenService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic, sendAccessToken: false)
        { }

        public async Task<DownPartyTestResultResponse> DownPartyTestResultAsync(DownPartyTestResultRequest downPartyTestResultRequest, string tenantName, string trackName) => await PutResponseAsync<DownPartyTestResultRequest, DownPartyTestResultResponse>(GetApiUrl(downPartyTestApiUri, tenantName, trackName), downPartyTestResultRequest);
    }
}
