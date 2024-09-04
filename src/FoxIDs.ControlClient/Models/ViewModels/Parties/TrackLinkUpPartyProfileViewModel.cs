using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class TrackLinkUpPartyProfileViewModel : UpPartyProfileViewModel
    {
        [ListLength(Constants.Models.TrackLinkDownParty.SelectedUpPartiesMin, Constants.Models.TrackLinkDownParty.SelectedUpPartiesMax, Constants.Models.Party.NameLength, Constants.Models.TrackLinkDownParty.SelectedUpPartiesNameRegExPattern)]
        [Display(Name = "Selected authentication methods (use * to select all authentication methods)")]
        public List<string> SelectedUpParties { get; set; } = new List<string>(["*"]);
    }
}
