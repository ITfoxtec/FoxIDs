using FoxIDs.Client.Shared.Components;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralUsageSettingsViewModel
    {
        public GeneralUsageSettingsViewModel()
        { }

        public string Error { get; set; }

        public PageEditForm<UsageSettingsViewModel> Form { get; set; }
    }
}
