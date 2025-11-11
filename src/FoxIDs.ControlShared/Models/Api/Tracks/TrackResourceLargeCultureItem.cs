using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TrackResourceLargeCultureItem
    {
        [Required]
        [MaxLength(Constants.Models.Resource.CultureLength)]
        public string Culture { get; set; }

        [MaxLength(Constants.Models.Resource.ResourceLarge.ValueLength)]
        public string Value { get; set; }
    }
}
