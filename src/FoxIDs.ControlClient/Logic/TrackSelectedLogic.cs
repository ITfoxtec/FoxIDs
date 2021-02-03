using FoxIDs.Models.Api;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Client.Logic
{
    public class TrackSelectedLogic
    {
        public Track Track { get; private set; }

        public bool IsTrackSelected => Track != null;

        public async Task TrackSelectedAsync(Track track)
        {
            Track = track;
            if(OnTrackSelectedAsync != null)
            {
                await OnTrackSelectedAsync(track);
            }
        }

        public event Func<Track, Task> OnTrackSelectedAsync;

        public async Task ShowSelectTrackAsync()
        {
            Track = null;
            if (OnShowSelectTrackAsync != null)
            {
                await OnShowSelectTrackAsync();
            }
        }

        public event Func<Task> OnShowSelectTrackAsync;
    }
}
