using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class WizardService : BaseService
    {
        private const string nemLoginSettingsApiUri = "api/{tenant}/{track}/!wizardnemloginsettings";
        private const string contextHandlerSettingsApiUri = "api/{tenant}/{track}/!wizardcontexthandlersettings";

        public WizardService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<WizardNemLoginSettings> ReadNemLoginSettingsAsync() => await GetAsync<WizardNemLoginSettings>(nemLoginSettingsApiUri);
        public async Task<WizardContextHandlerSettings> ReadContextHandlerSettingsAsync() => await GetAsync<WizardContextHandlerSettings>(contextHandlerSettingsApiUri);
    }
}
