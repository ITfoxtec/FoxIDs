using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TrackLargeResourceCultureItem
    {
        [Required]
        [MaxLength(Constants.Models.Resource.CultureLength)]
        public string Culture { get; set; }

        [MaxLength(Constants.Models.Resource.LargeResource.ValueLength)]
        public string Value { get; set; }
    }
}
