﻿using FoxIDs.Models.Api;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Client.Logic
{
    public class TrackSelectedLogic
    {
        public Track Track { get; private set; }

        public bool IsTrackSelected => Track != null;

        public event Func<Track, Task> OnTrackSelectedAsync;
        public event Func<Task> OnSelectTrackAsync;

        public async Task TrackSelectedAsync(Track track, bool isMasterTenant)
        {
            Track = track;
            if (!isMasterTenant)
            {
                await SaveTrackCookieAsync(track.Name);
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

        public async Task<string> ReadTrackCookieAsync()
        {

        }

        private async Task SaveTrackCookieAsync(string name)
        {

        }
    }
}
