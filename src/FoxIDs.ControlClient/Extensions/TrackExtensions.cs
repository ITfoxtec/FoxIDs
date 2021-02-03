using System;

namespace FoxIDs.Client
{
    public static class TrackExtensions
    {
        public static string FormatTrackName(this string trackName)
        {
            if ("-".Equals(trackName, StringComparison.Ordinal))
            {
                return "Production (-)";
            }

            return trackName;
        }
    }
}
