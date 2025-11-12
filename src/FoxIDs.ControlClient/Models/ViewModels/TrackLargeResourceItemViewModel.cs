using FoxIDs.Models.Api;
using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public class TrackLargeResourceItemViewModel : TrackLargeResourceItem
    {
        public TrackLargeResourceItemViewModel()
        {
            Items = new List<TrackLargeResourceCultureItem>();
        }
    }
}
