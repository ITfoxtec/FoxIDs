using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralResourceSettingsViewModel
    {
        public GeneralResourceSettingsViewModel()
        { }

        public bool Edit { get; set; }

        public string Error { get; set; }

        public PageEditForm<ResourceSettings> Form { get; set; }
    }
}
