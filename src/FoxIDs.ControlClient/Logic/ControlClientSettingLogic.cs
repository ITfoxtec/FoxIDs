using FoxIDs.Client.Models.Config;
using FoxIDs.Client.Services;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using System.Threading.Tasks;

namespace FoxIDs.Client.Logic
{
    public class ControlClientSettingLogic
    {
        private readonly ClientSettings clientSettings;
        private readonly ClientService clientService;
        private readonly NotificationLogic notificationLogic;

        public ControlClientSettingLogic(ClientSettings clientSettings, ClientService clientService, NotificationLogic notificationLogic)
        {
            this.clientSettings = clientSettings;
            this.clientService = clientService;
            this.notificationLogic = notificationLogic;
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
                clientSettings.EnableCreateNewTenant = controlClientSettings.EnableCreateNewTenant;
                clientSettings.EnablePayment = controlClientSettings.EnablePayment;
                clientSettings.PaymentTestMode = controlClientSettings.PaymentTestMode;
                clientSettings.MollieProfileId = controlClientSettings.MollieProfileId;
                clientSettings.ModuleAssets = controlClientSettings.ModuleAssets;

                clientSettings.HideBrandingSettings = controlClientSettings.ClientUi?.HideBrandingSettings ?? false;
                clientSettings.HideSmsSettings = controlClientSettings.ClientUi?.HideSmsSettings ?? false;
                clientSettings.HideMailSettings = controlClientSettings.ClientUi?.HideMailSettings ?? false;
                
                notificationLogic.ClientSettingLoaded();
            }
        }
    }
}
