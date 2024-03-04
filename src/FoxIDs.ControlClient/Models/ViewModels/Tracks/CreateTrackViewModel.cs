using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class CreateTrackViewModel
    {
        /// <summary>
        /// Environment name.
        /// </summary>
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameRegExPattern, ErrorMessage = "The field {0} can only contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Technical name")]
        public string Name { get; set; }

        /// <summary>
        /// Display name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Track.DisplayNameLength)]
        [RegularExpression(Constants.Models.Track.DisplayNameRegExPattern)]
        [Display(Name = "Name")]
        public string DisplayName { get; set; }

        public bool ShowAdvanced { get; set; }
    }
}
