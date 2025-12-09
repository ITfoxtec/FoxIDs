using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Text value for a specific culture.
    /// </summary>
    public class ResourceCultureItem
    {
        /// <summary>
        /// Culture code (e.g., en, da).
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Resource.CultureLength)]
        public string Culture { get; set; }

        /// <summary>
        /// Fallback value if a specific translation is missing.
        /// </summary>
        [MaxLength(Constants.Models.Resource.ValueLength)]
        public string DefaultValue { get; set; }

        /// <summary>
        /// Localized value for the culture.
        /// </summary>
        [MaxLength(Constants.Models.Resource.ValueLength)]
        public string Value { get; set; }
    }
}
