using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class TenantViewModel : Tenant
    {
        public string LoginUri { get; set; }
    }
}
