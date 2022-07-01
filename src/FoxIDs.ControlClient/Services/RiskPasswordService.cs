using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class RiskPasswordService : BaseService
    {
        private const string apiUri = "api/@master/!riskpassword";
        private const string infoApiUri = "api/@master/!riskpasswordinfo";

        public RiskPasswordService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<RiskPassword> GetRiskPasswordAsync(string passwordSha1Hash) => await GetAsync<RiskPassword>(apiUri, passwordSha1Hash, parmName: nameof(passwordSha1Hash));
        public async Task UpdateRiskPasswordAsync(RiskPasswordRequest riskPasswordRequest) => await PutAsync(apiUri, riskPasswordRequest);

        public async Task<RiskPasswordInfo> GetRiskPasswordInfoAsync() => await GetAsync<RiskPasswordInfo>(infoApiUri);
    }
}
