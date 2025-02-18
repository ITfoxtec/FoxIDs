using FoxIDs.Models.Api;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface IUpPartySelection
    {
        [Display(Name = "Authentication type")]
        PartyTypes? UpPartyType { get; set; }

        string UpPartyTypeText { get; set; }

        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        string UpPartyName { get; set; }

        [Display(Name = "Authentication name")]
        string UpPartyDisplayName { get; set; }

        [MaxLength(Constants.Models.Party.ProfileNameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        string UpPartyProfileName { get; set; }

        [Display(Name = "Authentication profile name")]
        string UpPartyDisplayProfileName { get; set; }
    }
}
