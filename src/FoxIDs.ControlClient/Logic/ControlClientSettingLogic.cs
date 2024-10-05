using FoxIDs.Client.Models.Config;
using FoxIDs.Client.Services;
using ITfoxtec.Identity;
using System.Threading.Tasks;

namespace FoxIDs.Client.Logic
{
    public class ControlClientSettingLogic
    {
        private readonly ClientSettings clientSettings;
        private readonly ClientService clientService;

        public ControlClientSettingLogic(ClientSettings clientSettings, ClientService clientService)
        {
            this.clientSettings = clientSettings;
            this.clientService = clientService;
        }

        public async Task InitLoadAsync()
        {
            if(clientSettings.FoxIDsEndpoint.IsNullOrEmpty())
            {
                var controlClientSettings = await clientService.GetControlClientSettingsAsync();
                clientSettings.FoxIDsEndpoint = controlClientSettings.FoxIDsEndpoint;
                clientSettings.Version = controlClientSettings.Version;
                clientSettings.FullVersion = controlClientSettings.FullVersion;
                clientSettings.LogOption = controlClientSettings.LogOption;
                clientSettings.KeyStorageOption = controlClientSettings.KeyStorageOption;
                clientSettings.EnablePlanPayment = controlClientSettings.EnablePlanPayment;
                clientSettings.Currency = controlClientSettings.Currency;
            }
        }
    }
}
