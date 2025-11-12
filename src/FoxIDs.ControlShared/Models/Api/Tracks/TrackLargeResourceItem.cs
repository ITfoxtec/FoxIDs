using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TrackLargeResourceItem : INameValue
    {
        [MaxLength(Constants.Models.Resource.LargeResource.IdLength)]
        public string Id { get; set; }

        [Required]
        [MaxLength(Constants.Models.Resource.NameLength)]
        [Display(Name = "Text look up key (en)")]
        public string Name { get; set; }

        [ListLength(Constants.Models.Resource.ResourcesMin, Constants.Models.Resource.ResourcesMax)]
        public List<TrackLargeResourceCultureItem> Items { get; set; }
    }
}
