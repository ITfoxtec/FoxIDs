using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Resource lookup key within a track.
    /// </summary>
    public class TrackResourceName
    {
        /// <summary>
        /// Text lookup key in English.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Resource.NameLength)]
        [Display(Name = "Text look up key (en)")]
        public string Name { get; set; }

        /// <summary>
        /// Numeric resource identifier.
        /// </summary>
        public int Id { get; set; }
    }
}
