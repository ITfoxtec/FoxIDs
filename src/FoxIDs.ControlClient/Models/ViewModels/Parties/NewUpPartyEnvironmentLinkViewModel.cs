using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class NewUpPartyEnvironmentLinkViewModel
    {
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

        [Display(Name = "Link to environment")]
        public string ToDownTrackDisplayName { get; set; }
    }
}
