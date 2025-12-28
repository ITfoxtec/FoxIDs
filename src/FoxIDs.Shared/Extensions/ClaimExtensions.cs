using FoxIDs.Models;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace FoxIDs
{
    public static class ClaimExtensions
    {
        /// <summary>
        /// Retrieves the first claim value that is matched by the specified predicate.
        /// </summary>
        /// <param name="claims">A claim collection.</param>
        /// <param name="predicate">The function that performs the matching logic.</param>
        public static string FindFirstOrDefaultValue(this IEnumerable<Claim> claims, Func<Claim, bool> predicate)
        {
            return claims?.Where(predicate)?.Select(c => c.Value).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves the first claim value that is matched by the specified predicate.
        /// </summary>
        /// <param name="claims">A claim collection.</param>
        /// <param name="predicate">The function that performs the matching logic.</param>
        public static string FindFirstOrDefaultValue(this IEnumerable<ClaimAndValues> claims, Func<ClaimAndValues, bool> predicate)
        {
            return claims?.Where(predicate)?.Select(c => c.Values?.FirstOrDefault()).FirstOrDefault();
        }

        /// <summary>
        /// Add Claim to List&lt;Claim&gt;.
        /// </summary>
        public static void AddClaim(this List<Claim> list, string type, string value)
        {
            list.Add(new Claim(type, value));
        }

        /// <summary>
        /// Add or replace Claim to List&lt;Claim&gt;.
        /// </summary>
        public static void AddOrReplaceClaim(this List<Claim> list, string type, string value)
        {
            if (list.Where(c => c.Type == type).Any())
            {
                list.RemoveAll(c => c.Type == type);
            }
            list.Add(new Claim(type, value));
        }

        /// <summary>
        /// Add Claim to List&lt;Claim&gt;.
        /// </summary>
        public static void AddClaim(this List<Claim> list, string type, string value, string valueType, string issuer)
        {
            list.Add(new Claim(type, value, valueType, issuer));
        }

        /// <summary>
        /// Converts an Dictionary&lt;string, List&lt;string&gt;&gt; to a Claims list.
        /// </summary>
        public static List<Claim> ToClaimList(this Dictionary<string, List<string>> list)
        {
            return list.SelectMany(item => item.Value.Select(value => new Claim(item.Key, value))).ToList();
        }

        /// <summary>
        /// Converts a ClaimAndValues list to a Claims list.
        /// </summary>
        public static List<Claim> ToClaimList(this IEnumerable<ClaimAndValues> list)
        {
            return list.SelectMany(item => item.Values.Select(value => new Claim(item.Claim, value))).ToList();
        }

        /// <summary>
        /// Converts a Claims list to an Dictionary&lt;string, List&lt;string&gt;&gt;.
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
            foreach (var gc in list.Where(c => !c.Value.IsNullOrWhiteSpace()).GroupBy(c => c.Type))
            {
                claimAndValues.Add(new ClaimAndValues { Claim = gc.Key, Values = gc.Select(gci => gci.Value).ToList() });
            }
            return claimAndValues;
        }

        public static string ToFormattedString(this IEnumerable<Claim> list)
        {
            if (list != null)
            {
                return string.Join(", ", list.Select(c => $"{c.Type}: {MaskSensitiveClaimValue(c.Type, c.Value)}"));
            }
            return string.Empty;
        }

        private static string MaskSensitiveClaimValue(string type, string value)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return value;
            }

            if (type == Constants.JwtClaimTypes.CprNumber || type == Constants.SamlClaimTypes.CprNumber)
            {
                return value.MaskCprNumber();
            }

            return value;
        }

        public static string MaskCprNumber(this string cprNumber)
        {
            if (cprNumber.IsNullOrWhiteSpace() || cprNumber.Length != 10)
            {
                return cprNumber;
            }

            return $"{cprNumber.Substring(0, 6)}****";
        }

        /// <summary>
        /// Concatenates two sequences and only include a claim from the second sequence if not already in the first sequence.
        /// </summary>
        /// <param name="first">The first sequence to concatenate.</param>
        /// <param name="second">The sequence to concatenate to the first sequence.</param>
        public static IEnumerable<Claim> ConcatOnce(this IEnumerable<Claim> first, IEnumerable<Claim> second)
        {           
            return first.ConcatOnce(second, (f, s) => f.Type == s.Type);
        }
    }
}
