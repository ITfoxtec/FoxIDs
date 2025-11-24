using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TrackLargeResourceItem : INameValue
    {
        [Required]
        [MinLength(Constants.Models.Resource.LargeResource.NameMinLength)]
        [MaxLength(Constants.Models.Resource.LargeResource.NameMaxLength)]
        [Display(Name = "Unique look up name")]
        public string Name { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.Resource.ResourcesMin, Constants.Models.Resource.ResourcesMax)]
        [Display(Name = "Texts")]
        public List<TrackLargeResourceCultureItem> Items { get; set; }
    }
}
