using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TrackLinkUpPartyProfile : IProfile
    {
        [Required]
        [MaxLength(Constants.Models.Party.ProfileNameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.ProfileNameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string NewName { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        public string DisplayName { get; set; }

        [ListLength(Constants.Models.TrackLinkDownParty.SelectedUpPartiesMin, Constants.Models.TrackLinkDownParty.SelectedUpPartiesMax, Constants.Models.Party.NameLength, Constants.Models.TrackLinkDownParty.SelectedUpPartiesNameRegExPattern)]
        public List<string> SelectedUpParties { get; set; }
    }
}
