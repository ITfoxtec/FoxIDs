using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class ClientService : BaseService
    {
        private const string clientConfigApiUri = "api/@master/!clientsettings";

        public ClientService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic, sendAccessToken: false)
        { }

        public async Task<ControlClientSettings> GetControlClientSettingsAsync() => await GetAsync<ControlClientSettings>(clientConfigApiUri);
    }
}
