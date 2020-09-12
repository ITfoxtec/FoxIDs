namespace FoxIDs.Client.Models.Config
{
    public class ClientSettings
    {
        public string FoxIDsEndpoint { get; set; }
        public string Authority { get; set; }
        public string LoginCallBackPath { get; set; }
        public string LogoutCallBackPath { get; set; }
    }
}
