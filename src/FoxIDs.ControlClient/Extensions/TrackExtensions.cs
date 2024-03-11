using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Client
{
    public static class TrackExtensions
    {
        public static string GetProdTrackName(this string trackName)
        {
            if ("-".Equals(trackName, StringComparison.Ordinal))
            {
                return "(dash is production)";
            }

            return string.Empty;
        }

        public static string GetConcatProdTrackName(this string trackName)
        {
            var prodText = trackName.GetProdTrackName();
            if (prodText.IsNullOrWhiteSpace()) 
            {
                return trackName;
            }
            else
            {
                return $"{trackName} {prodText}";
            }
        }

        public static IEnumerable<Track> OrderTracks(this IEnumerable<Track> tracks)
        {
            var orderedTracks = new List<Track>();
            if (tracks?.Count() > 0)
            {
                Track masterTrack = null;
                foreach (var track in tracks)
                {
                    if (Constants.Routes.MasterTenantName.Equals(track.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        masterTrack = track;
                    }
                    else
                    {
                        orderedTracks.Add(track);
                    }
                }
                if (masterTrack != null)
                {
                    orderedTracks.Add(masterTrack);
                }
            }
            return orderedTracks;
        }
    }
}
