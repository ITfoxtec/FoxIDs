using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Culture entry used when registering available resource cultures.
    /// </summary>
    public class ResourceCulture
    {
        /// <summary>
        /// Culture code (e.g., en, da).
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Resource.CultureLength)]
        public string Culture { get; set; }
    }
}
