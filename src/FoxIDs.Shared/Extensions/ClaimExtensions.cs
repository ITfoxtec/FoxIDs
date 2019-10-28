using FoxIDs.Models;
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
        public static string FindFirstValue(this IEnumerable<Claim> claims, Func<Claim, bool> predicate)
        {
            return claims.Where(predicate).Select(c => c.Value).FirstOrDefault();
        }

        /// <summary>
        /// Add Claim to List&lt;Claim&gt;.
        /// </summary>
        public static void AddClaim(this List<Claim> list, string type, string value)
        {
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
        public static List<Claim> ToClaimList(this List<ClaimAndValues> list)
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
            foreach (var gc in list.GroupBy(c => c.Type))
            {
                claimAndValues.Add(new ClaimAndValues { Claim = gc.Key, Values = gc.Select(gci => gci.Value).ToList() });
            }
            return claimAndValues;
        }
    }
}
