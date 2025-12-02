using System;
using System.Globalization;

namespace FoxIDs.Client
{
    public static class TimeExtensions
    {
        public static string TimeToString(this long time)
        {
            var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(time);
            return dateTimeOffset.ToLocalTime().ToString(CultureInfo.InvariantCulture);
        }

        public static string ToDateText(this DateTime dateTime) => dateTime.ToString(CultureInfo.InvariantCulture);

        public static string ToDateText(this DateTimeOffset dateTimeOffset) => dateTimeOffset.ToString(CultureInfo.InvariantCulture);
    }
}
