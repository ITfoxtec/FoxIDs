using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public abstract class UpPartyProfileViewModel
    {
        [Required]
        [MaxLength(Constants.Models.Party.ProfileNameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        public string DisplayName { get; set; }
    }
}
