using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralUsedViewModel : Used
    {
        public GeneralUsedViewModel()
        { }

        public GeneralUsedViewModel(UsedBase used)
        {
            TenantName = used.TenantName;
            Year = used.Year;
            Month = used.Month;
            TotalPrice = used.TotalPrice;
        }

        public bool Edit { get; set; }

        public bool ShowAdvanced { get; set; }

        public bool CreateMode { get; set; }

        public string Error { get; set; }

        public PageEditForm<UsedViewModel> Form { get; set; }
    }
}
