using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Api = FoxIDs.Models.Api;

namespace FoxIDs
{
    public static class ClaimExtensions
    {
        /// <summary>
        /// Converts a ClaimAndValues list to a Claims list.
        /// </summary>
        public static List<Claim> ToClaimList(this IEnumerable<Api.ClaimAndValues> list)
        {
            if (list?.Count() > 0)
            {
                return list.SelectMany(item => item.Values.Select(value => new Claim(item.Claim, value))).ToList();
            }
            else
            {
                return null;
            }
        }
    }
}
