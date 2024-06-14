using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class DownParty
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        public string DisplayName { get; set; }

        [Required]
        public PartyTypes Type { get; set; }

        /// <summary>
        /// Is test.
        /// </summary>
        public bool IsTest { get; set; }
    }
}
