using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Reference to an upstream party and optional profile.
    /// </summary>
    public class UpPartyLink
    {
        /// <summary>
        /// Upstream party name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        /// <summary>
        /// Optional profile name to use for the party.
        /// </summary>
        [MaxLength(Constants.Models.Party.ProfileNameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string ProfileName { get; set; }
    }
}
