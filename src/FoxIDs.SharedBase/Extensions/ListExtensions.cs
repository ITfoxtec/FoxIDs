using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs
{
    /// <summary>
    /// Extension methods for arrays and dictionary's.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Converts a string list to a dot separated list.
        /// </summary>
        public static string ToDotList(this string[] values)
        {
            if (values?.Count() > 0)
            {
                return string.Join('.', values);
            }
            return null;
        }

        /// <summary>
        /// Converts a dot separated list to a string list.
        /// </summary>
        public static string[] FromDotList(this string value)
        {
            if (!value.IsNullOrWhiteSpace())
            {
                return value.Split('.');
            }
            return null;
        }

        /// <summary>
        /// Return first element in a dot separated list.
        /// </summary>
        public static string GetFirstInDotList(this string value)
        {
            return value.FromDotList()?.FirstOrDefault() ?? value;
        }

        /// <summary>
        /// Return last element in a dot separated list.
        /// </summary>
        public static string GetLastInDotList(this string value)
        {
            return value.FromDotList()?.LastOrDefault() ?? value;
        }

        /// <summary>
        /// Concatenates two sequences and only include each string value once.
        /// </summary>
        /// <param name="first">The first sequence to concatenate.</param>
        /// <param name="second">The sequence to concatenate to the first sequence.</param>
        public static List<string> ConcatOnce(this IEnumerable<string> first, IEnumerable<string> second)
        {
            var list = first != null ? new List<string>(first) : new List<string>();
            if(second?.Count() > 0)
            {
                list.AddRange(second.Where(vc => !list.Contains(vc)));
            }
            return list;
        }

        /// <summary>
        /// Concatenates two sequences and only include each string value once.
        /// </summary>
        /// <param name="first">The first sequence to concatenate.</param>
        /// <param name="second">The sequence to concatenate to the first sequence.</param>
        public static List<string> ConcatOnce(this List<string> first, List<string> second)
        {
            var list = first ?? new List<string>();
            if (second != null)
            {
                list.AddRange(second.Where(vc => !list.Contains(vc)));
            }
            return list;
        }

        /// <summary>
        /// Concatenates two sequences and only include each key once.
        /// </summary>
        /// <param name="first">The first sequence to concatenate.</param>
        /// <param name="second">The sequence to concatenate to the first sequence.</param>
        public static IDictionary<string, string> ConcatOnce(this IDictionary<string, string> first, IDictionary<string, string> second)
        {
            var list = first ?? new Dictionary<string, string>();
            if (second != null)
            {
                foreach(var s in second)
                {
                    if(!list.ContainsKey(s.Key))
                    {
                        list.Add(s.Key, s.Value);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Concatenates two sequences and only include each item once.
        /// </summary>
        /// <param name="first">The first sequence to concatenate.</param>
        /// <param name="second">The sequence to concatenate to the first sequence.</param>
        /// <param name="compare">Compare the first and second sequence and add not equal items.</param>
        public static IEnumerable<T> ConcatOnce<T>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, T, bool> compare)
        {
            var list = first == null ? new List<T>() : new List<T>(first);
            if (second != null)
            {
                list.AddRange(second.Where(vc => !list.Any(m => compare(m, vc))));
            }
            return list;
        }
    }
}
