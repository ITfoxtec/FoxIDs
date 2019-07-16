using ITfoxtec.Identity;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs
{
    /// <summary>
    /// Extension methods for arrays and dictionarys.
    /// </summary>
    public static class List
    {
        /// <summary>
        /// Add Claim to List<Claim>.
        /// </summary>
        public static void AddClaim(this List<Claim> list, string type, string value)
        {
            list.Add(new Claim(type, value));
        }

        /// <summary>
        /// Add Claim to List<Claim>.
        /// </summary>
        public static void AddClaim(this List<Claim> list, string type, string value, string valueType, string issuer)
        {
            list.Add(new Claim(type, value, valueType, issuer));
        }

        /// <summary>
        /// Converts an Dictionary<string, List<string>> to a Claims list.
        /// </summary>
        public static List<Claim> ToClaimList(this Dictionary<string, List<string>> list)
        {
            return list.SelectMany(item => item.Value.Select(value => new Claim(item.Key, value))).ToList();            
        }

        /// <summary>
        /// Converts a ClaimAndValues list to a Claims list.
        /// </summary>
        public static List<Claim> ToClaimList(this List<ClaimAndValues> list)
        {
            return list.SelectMany(item => item.Values.Select(value => new Claim(item.Claim, value))).ToList();
        }

        /// <summary>
        /// Converts a Claims list to an Dictionary<string, List<string>>.
        /// </summary>
        public static Dictionary<string, List<string>> ToDictionary(this List<Claim> list)
        {
            var dictionary = new Dictionary<string, List<string>>();
            foreach (var gc in list.GroupBy(c => c.Type))
            {
                dictionary.Add(gc.Key, gc.Select(gci => gci.Value).ToList());
            }
            return dictionary;
        }

        /// <summary>
        /// Converts a Claims list to a ClaimAndValues list.
        /// </summary>
        public static List<ClaimAndValues> ToClaimAndValues(this IEnumerable<Claim> list)
        {
            var claimAndValues = new List<ClaimAndValues>();
            foreach (var gc in list.GroupBy(c => c.Type))
            {
                claimAndValues.Add(new ClaimAndValues { Claim = gc.Key, Values = gc.Select(gci => gci.Value).ToList() });
            }
            return claimAndValues;
        }

        /// <summary>
        /// Concatenates two sequences and only include each string value once.
        /// </summary>
        /// <param name="first">The first sequence to concatenate.</param>
        /// <param name="second">The sequence to concatenate to the first sequence.</param>
        public static List<string> ConcatOnce(this IEnumerable<string> first, IEnumerable<string> second)
        {
            var list = first == null ? new List<string>() : new List<string>(first);
            if(second != null)
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
        /// Converts an IFormCollection to a Dictionary<string, string>.
        /// </summary>
        public static Dictionary<string, string> ToDictionary(this IFormCollection list)
        {
            return list.ToDictionary(x => x.Key, x => x.Value.FirstOrDefault());
        }

        /// <summary>
        /// Converts an Dictionary<string, StringValues> to a Dictionary<string, string>.
        /// </summary>
        public static Dictionary<string, string> ToDictionary(this Dictionary<string, StringValues> list)
        {
            return list.ToDictionary(x => x.Key, x => x.Value.FirstOrDefault());
        }

        /// <summary>
        /// Converts a Dictionary<string, string> to a HTML Post Content Result.
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
        /// Converts a Dictionary<string, string> to a Redirect Result.
        /// </summary>
        public static Task<RedirectResult> ToRedirectResultAsync(this Dictionary<string, string> items, string url)
        {
            return Task.FromResult(new RedirectResult(QueryHelpers.AddQueryString(url, items)));
        }

        /// <summary>
        /// Converts a Dictionary<string, string> to a Fragment Result.
        /// </summary>
        public static Task<RedirectResult> ToFragmentResultAsync(this Dictionary<string, string> items, string url)
        {
            return Task.FromResult(new RedirectResult(QueryHelpers.AddQueryString(url, items).Replace('?', '#')));
        }
    }
}
