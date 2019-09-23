using System;
using ITfoxtec.Identity;

namespace FoxIDs
{
    /// <summary>
    /// Extension methods for strings.
    /// </summary>
    public static class StringExtensions
    {


        ///// <summary>
        ///// Returns a new string in which all occurrences of a specified string in the current instance are replaced with another specified string.
        ///// </summary>
        ///// <param name="oldValue">The string to be replaced.</param>
        ///// <param name="newValue">The string to replace all occurrences of oldValue.</param>
        ///// <param name="comparisonType">One of the enumeration values that determines how this string and value are compared.</param>
        ///// <returns></returns>
        //public static string Replace(this string str, string oldValue, string newValue, StringComparison comparisonType)
        //{
        //    newValue = newValue ?? string.Empty;
        //    if (str.IsNullOrEmpty() || oldValue.IsNullOrEmpty() || oldValue.Equals(@newValue, comparisonType))
        //    {
        //        return str;
        //    }
        //    int foundAt;
        //    while ((foundAt = str.IndexOf(oldValue, 0, comparisonType)) != -1)
        //    {
        //        str = str.Remove(foundAt, oldValue.Length).Insert(foundAt, @newValue);
        //    }
        //    return str;
        //}

        ///// <summary>
        ///// Determines whether the beginning of this string instance matches the specified char.
        ///// </summary>
        ///// <param name="str">The char to compare.</param>
        ///// <returns>true if value matches the beginning of this string; otherwise, false.</returns>
        //public static bool StartsWith(this string str, char value)
        //{
        //    if (str.IsNullOrWhiteSpace()) return false;

        //    return str.IndexOf(value) == 0;
        //}
    }
}
