using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.External
{
    public class ClaimsResponse
    {
        [ListLength(Constants.ExternalConnect.ClaimsMin, Constants.ExternalConnect.ClaimsMax)]
        public IEnumerable<ClaimValue> Claims { get; set; }
    }
}
