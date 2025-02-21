using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public abstract class UpPartyProfileViewModel
    {
        public string InitName { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.ProfileNameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [Display(Name = "Technical name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        [Display(Name = "Name")]
        public string DisplayName { get; set; }
    }
}
