using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class NewUpPartyEnvironmentLinkViewModel
    {
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Technical name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        [Display(Name = "Name")]
        public string DisplayName { get; set; }

        [Required(ErrorMessage = "Select what environment the Environment Link should link to.")]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameDbRegExPattern)]
        [Display(Name = "Link to environment")]
        public string ToDownTrackName { get; set; }

        public string ToDownTrackDisplayName { get; set; }
    }
}
