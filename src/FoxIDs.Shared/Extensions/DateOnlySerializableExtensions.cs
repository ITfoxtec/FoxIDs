using FoxIDs.Models;
using System;

namespace FoxIDs
{
    public static class DateOnlySerializableExtensions
    {
        public static DateOnlySerializable AddDays(this DateOnlySerializable dateOnlySez, int value)
        {
            return ToDateOnlySerializable(new DateOnly(dateOnlySez.Year, dateOnlySez.Month, dateOnlySez.Day).AddDays(value));
        }

        public static DateOnlySerializable AddMonths(this DateOnlySerializable dateOnlySez, int value)
        {
            return ToDateOnlySerializable(new DateOnly(dateOnlySez.Year, dateOnlySez.Month, dateOnlySez.Day).AddMonths(value));
        }

        public static int GetDayNumber(this DateOnlySerializable dateOnlySez)
        {
            return dateOnlySez.ToDateOnly().DayNumber;
        }

        public static DateOnly ToDateOnly(this DateOnlySerializable dateOnlySez) => new DateOnly(dateOnlySez.Year, dateOnlySez.Month, dateOnlySez.Day);

        public static DateOnlySerializable ToDateOnlySerializable(this DateOnly dateOnly) => new DateOnlySerializable(dateOnly.Year, dateOnly.Month, dateOnly.Day);
    }
}
