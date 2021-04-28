using System;

namespace FoxIDs.Client
{
    public static class TimeExtensions
    {
        public static string TimeToString(this long time)
        {
            var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(time);
            return dateTimeOffset.ToLocalTime().ToString();
        }
    }
}
