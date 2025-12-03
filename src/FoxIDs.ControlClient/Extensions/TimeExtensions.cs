using System;
using System.Globalization;

namespace FoxIDs.Client
{
    public static class TimeExtensions
    {
        public static string TimeToString(this long time)
        {
            var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(time);
            return dateTimeOffset.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
        }

        public static string ToDateText(this DateTime dateTime) => dateTime.ToString("dd/MM/yyyy HH:mm:ss");

        public static string ToDateText(this DateTimeOffset dateTimeOffset) => dateTimeOffset.ToString("dd/MM/yyyy HH:mm:ss");
    }
}
