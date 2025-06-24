using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralTrackResourceViewModel
    {
        public GeneralTrackResourceViewModel(TrackResourceName resourceName)
        {
            Id = resourceName.Id;
            Name = resourceName.Name;
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public bool Edit { get; set; }

        public bool CreateMode { get; set; }

        public bool ShowAdvanced { get; set; }

        public bool DeleteAcknowledge { get; set; }

        public string Error { get; set; }

        public PageEditForm<ResourceItemViewModel> Form { get; set; }
    }
}
