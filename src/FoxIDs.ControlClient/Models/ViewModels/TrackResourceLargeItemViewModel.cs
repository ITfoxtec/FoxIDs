using FoxIDs.Models.Api;
using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public class TrackResourceLargeItemViewModel : TrackResourceLargeItem
    {
        public TrackResourceLargeItemViewModel()
        {
            Items = new List<TrackResourceLargeCultureItem>();
        }
    }
}
