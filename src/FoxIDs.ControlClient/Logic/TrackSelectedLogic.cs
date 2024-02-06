using FoxIDs.Models.Api;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Client.Logic
{
    public class TrackSelectedLogic
    {
        [Inject]
        public UserProfileLogic UserProfileLogic { get; set; }

        public Track Track { get; private set; }

        public bool IsTrackSelected => Track != null;

        public event Func<Track, Task> OnTrackSelectedAsync;
        public event Func<Task> OnSelectTrackAsync;

        public async Task TrackSelectedAsync(Track track, bool isMasterTenant)
        {
            Track = track;
            if (!isMasterTenant)
            {
                await UserProfileLogic.UpdateTrackAsync(track.Name);
            }
            if (OnTrackSelectedAsync != null)
            {
                await OnTrackSelectedAsync(track);
            }
        }

        public async Task StartSelectTrackAsync()
        {
            Track = null;
            if (OnSelectTrackAsync != null)
            {
                await OnSelectTrackAsync();
            }
        }

        public async Task<string> ReadTrackFromUserProfileAsync(string userSub)
        {
            var userProfile = await UserProfileLogic.GetUserProfileAsync(userSub);
            return userProfile?.LastTrackName;
        }
    }
}
