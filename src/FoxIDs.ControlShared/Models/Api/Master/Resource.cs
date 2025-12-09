using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Collection of localized resource values.
    /// </summary>
    public class Resource
    {
        /// <summary>
        /// Cultures supported for the resource entries.
        /// </summary>
        [ListLength(Constants.Models.Resource.SupportedCulturesMin, Constants.Models.Resource.SupportedCulturesMax, Constants.Models.Resource.SupportedCulturesLength)]
        public List<string> SupportedCultures { get; set; }

        /// <summary>
        /// Resource name definitions per culture.
        /// </summary>
        [ListLength(Constants.Models.Resource.ResourcesMin, Constants.Models.Resource.ResourcesMax)]
        public List<ResourceName> Names { get; set; }

        /// <summary>
        /// Resource values per culture.
        /// </summary>
        [ListLength(Constants.Models.Resource.ResourcesMin, Constants.Models.Resource.ResourcesMax)]
        public List<ResourceItem> Resources { get; set; }
    }
}
