using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Large text resource entry for a track.
    /// </summary>
    public class TrackLargeResourceItem : INameValue
    {
        /// <summary>
        /// Unique lookup name for the resource.
        /// </summary>
        [Required]
        [MinLength(Constants.Models.Resource.LargeResource.NameMinLength)]
        [MaxLength(Constants.Models.Resource.LargeResource.NameMaxLength)]
        [Display(Name = "Unique look up name")]
        public string Name { get; set; }

        /// <summary>
        /// Localized text values.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.Resource.ResourcesMin, Constants.Models.Resource.ResourcesMax)]
        [Display(Name = "Texts")]
        public List<TrackLargeResourceCultureItem> Items { get; set; }
    }
}
