using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Upstream party reference with available profiles.
    /// </summary>
    public class UpParty
    {
        /// <summary>
        /// Technical party name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        /// <summary>
        /// Display friendly party name.
        /// </summary>
        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Type of party.
        /// </summary>
        [Required]
        public PartyTypes Type { get; set; }

        /// <summary>
        /// Available profiles for the party.
        /// </summary>
        public List<UpPartyProfile> Profiles { get; set; }
    }
}
