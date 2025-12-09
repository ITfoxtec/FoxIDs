using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Localized large text value for a specific culture.
    /// </summary>
    public class TrackLargeResourceCultureItem
    {
        /// <summary>
        /// Culture code (e.g., en, da-DK).
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Resource.CultureLength)]
        public string Culture { get; set; }

        /// <summary>
        /// Text value for the culture.
        /// </summary>
        [MaxLength(Constants.Models.Resource.LargeResource.ValueLength)]
        public string Value { get; set; }
    }
}
