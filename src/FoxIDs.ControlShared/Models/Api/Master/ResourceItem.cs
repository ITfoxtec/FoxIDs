using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Localized resource text collection grouped by identifier.
    /// </summary>
    public class ResourceItem
    {
        /// <summary>
        /// Shared resource identifier.
        /// </summary>
        [Required]
        [Display(Name = "Id")]
        public int Id { get; set; }

        /// <summary>
        /// Localized values for the resource.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.Resource.ResourcesMin, Constants.Models.Resource.ResourcesMax)]
        [Display(Name = "Texts")]
        public List<ResourceCultureItem> Items { get; set; }
    }
}
