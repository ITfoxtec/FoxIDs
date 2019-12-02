using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs
{
    /// <summary>
    /// Extension methods for arrays and dictionarys.
    /// </summary>
    public static class List
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
        public static string[] ToDotList(this string value)
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
            return value.ToDotList()?.FirstOrDefault() ?? value;
        }

        /// <summary>
        /// Return last element in a dot separated list.
        /// </summary>
        public static string GetLastInDotList(this string value)
        {
            return value.ToDotList()?.LastOrDefault() ?? value;
        }

        /// <summary>
        /// Concatenates two sequences and only include each string value once.
        /// </summary>
        /// <param name="first">The first sequence to concatenate.</param>
        /// <param name="second">The sequence to concatenate to the first sequence.</param>
        public static List<string> ConcatOnce(this IEnumerable<string> first, IEnumerable<string> second)
        {
            var list = first == null ? new List<string>() : new List<string>(first);
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
            var list = first == null ? new List<string>() : first;
            if (second != null)
            {
                list.AddRange(second.Where(vc => !list.Contains(vc)));
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

        /// <summary>
        /// Converts an IFormCollection to a Dictionary&lt;string, string&gt;.
        /// </summary>
        public static Dictionary<string, string> ToDictionary(this IFormCollection list)
        {
            return list.ToDictionary(x => x.Key, x => x.Value.FirstOrDefault());
        }

        /// <summary>
        /// Converts an Dictionary&lt;string, StringValues&gt; to a Dictionary&lt;string, string&gt;.
        /// </summary>
        public static Dictionary<string, string> ToDictionary(this Dictionary<string, StringValues> list)
        {
            return list.ToDictionary(x => x.Key, x => x.Value.FirstOrDefault());
        }

        /// <summary>
        /// Converts a Dictionary&lt;string, string&gt; to a HTML Post Content Result.
        /// </summary>
        public static Task<ContentResult> ToHtmlPostContentResultAsync(this Dictionary<string, string> items, string url)
        {
            return Task.FromResult(new ContentResult
            {
                ContentType = "text/html",
                Content = items.ToHtmlPostPage(url),
            });
        }

        /// <summary>
        /// Converts a Dictionary&lt;string, string&gt; to a Redirect Result.
        /// </summary>
        public static Task<RedirectResult> ToRedirectResultAsync(this Dictionary<string, string> items, string url)
        {
            return Task.FromResult(new RedirectResult(QueryHelpers.AddQueryString(url, items)));
        }

        /// <summary>
        /// Converts a Dictionary&lt;string, string&gt; to a Fragment Result.
        /// </summary>
        public static Task<RedirectResult> ToFragmentResultAsync(this Dictionary<string, string> items, string url)
        {
            return Task.FromResult(new RedirectResult(QueryHelpers.AddQueryString(url, items).Replace('?', '#')));
        }
    }
}
