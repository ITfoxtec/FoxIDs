using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralTenantViewModel : Tenant
    {
        public GeneralTenantViewModel()
        { }

        public GeneralTenantViewModel(Tenant tenant)
        {
            Name = tenant.Name;
        }

        public bool Edit { get; set; }

        public bool ShowAdvanced { get; set; }

        public bool DeleteAcknowledge { get; set; }

        public string Error { get; set; }

        public PageEditForm<TenantViewModel> Form { get; set; }
    }
}
