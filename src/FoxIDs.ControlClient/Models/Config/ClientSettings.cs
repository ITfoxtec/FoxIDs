namespace FoxIDs.Client.Models.Config
{
    public class ClientSettings
    {
        public string FoxIDsEndpoint { get; set; }
        public string Authority { get; set; }
        public string ClientId { get; set; }
        public string Scope { get; set; }
        public string MasterScope { get; set; }
        public string LoginCallBackPath { get; set; }
        public string LogoutCallBackPath { get; set; }
    }
}
