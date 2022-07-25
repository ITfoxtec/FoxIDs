using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class RiskPasswordService : BaseService
    {
        private const string apiUri = "api/@master/!riskpasswordtest";
        private const string infoApiUri = "api/@master/!riskpasswordinfo";

        public RiskPasswordService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<bool> GetRiskPasswordTestAsync(string password) => await GetAsync<bool>(apiUri, password, parmName: nameof(password));

        public async Task<RiskPasswordInfo> GetRiskPasswordInfoAsync() => await GetAsync<RiskPasswordInfo>(infoApiUri);
    }
}
