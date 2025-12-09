using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Identifies a resource entry and its lookup key.
    /// </summary>
    public class ResourceName
    {
        /// <summary>
        /// Text lookup key in English (en).
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Resource.NameLength)]
        [Display(Name = "Text look up key (en)")]
        public string Name { get; set; }

        /// <summary>
        /// Numeric identifier shared across cultures.
        /// </summary>
        [Required]
        public int Id { get; set; }
    }
}
