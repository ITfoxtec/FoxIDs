using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;

namespace FoxIDs.Client.Infrastructure.Security
{
    public class TenantOpenidConnectPkceSettings : OpenidConnectPkceSettings
    {
        public string FoxIDsEndpoint { get; set; }
        public string MasterScope { get; set; }
    }
}
