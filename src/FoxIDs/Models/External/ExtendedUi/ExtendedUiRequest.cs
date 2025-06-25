using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.External
{
    public class ExtendedUiRequest
    {
        [ListLength(Constants.Models.DynamicElements.ElementsMin, Constants.Models.DynamicElements.ElementsMax)]
        public IEnumerable<ElementValue> Elements { get; set; }

        [ListLength(Constants.ExternalConnect.ClaimsMin, Constants.ExternalConnect.ClaimsMax)]
        public IEnumerable<ClaimValue> Claims { get; set; }
    }
}
