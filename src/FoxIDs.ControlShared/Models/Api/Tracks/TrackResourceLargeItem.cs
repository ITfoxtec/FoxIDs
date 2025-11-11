using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TrackResourceLargeItem : INameValue
    {
        [MaxLength(Constants.Models.Resource.ResourceLarge.IdLength)]
        public string Id { get; set; }

        [Required]
        [MaxLength(Constants.Models.Resource.NameLength)]
        [Display(Name = "Text look up key (en)")]
        public string Name { get; set; }

        [ListLength(Constants.Models.Resource.ResourcesMin, Constants.Models.Resource.ResourcesMax)]
        public List<TrackResourceLargeCultureItem> Items { get; set; }
    }
}
