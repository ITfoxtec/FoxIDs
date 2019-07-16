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
        /// <returns></returns>
        public static string FindFirstValue(this IEnumerable<Claim> claims, Func<Claim, bool> predicate)
        {
            return claims.Where(predicate).Select(c => c.Value).FirstOrDefault();
        }
    }
}
