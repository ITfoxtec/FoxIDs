using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class SmsSettingsViewModel : SendSms
    {
        //public SendSmsTypes Type { get; set; }
        //public string FromName { get; set; }
        //public string ApiUrl { get; set; }
        //public string ClientId { get; set; }
        //public string ClientSecret { get; set; }

        public string KeyJson { get; set; }
    }
}
