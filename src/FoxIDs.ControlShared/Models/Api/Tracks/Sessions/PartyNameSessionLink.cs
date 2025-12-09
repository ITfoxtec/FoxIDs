using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// References a party and type participating in a session.
    /// </summary>
    public class PartyNameSessionLink
    {
        /// <summary>
        /// Party name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        /// <summary>
        /// Party type.
        /// </summary>
        [Required]
        public PartyTypes Type { get; set; }
    }
}
