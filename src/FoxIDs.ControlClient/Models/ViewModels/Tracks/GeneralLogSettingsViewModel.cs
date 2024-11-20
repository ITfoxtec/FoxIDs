using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralLogSettingsViewModel
    {
        public GeneralLogSettingsViewModel()
        { }

        public string Error { get; set; }

        public PageEditForm<LogSettings> Form { get; set; }
    }
}
