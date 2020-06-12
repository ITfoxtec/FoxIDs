using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;

namespace FoxIDs.Infrastructure.Security
{
    public class TenantOpenidConnectPkceSettings : OpenidConnectPkceSettings
    {
        public string MasterScope { get; set; }
    }
}
