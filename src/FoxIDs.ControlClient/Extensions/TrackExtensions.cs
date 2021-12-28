using System;

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

            return String.Empty;
        }

        public static string FormatTrackName(this string trackName)
        {
            if ("-".Equals(trackName, StringComparison.Ordinal))
            {
                return "- (dash is production)";
            }

            return trackName;
        }
    }
}
