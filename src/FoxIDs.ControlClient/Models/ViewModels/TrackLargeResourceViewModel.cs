using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class TrackLargeResourceViewModel
    {
        public TrackLargeResourceViewModel()
        {
        }

        public TrackLargeResourceViewModel(TrackLargeResourceItem resource)
        {
            Id = resource.Id;
            Name = resource.Name;
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public bool Edit { get; set; }

        public bool CreateMode { get; set; }

        public bool ShowAdvanced { get; set; }

        public bool DeleteAcknowledge { get; set; }

        public string Error { get; set; }

        public PageEditForm<TrackLargeResourceItemViewModel> Form { get; set; }
    }
}
