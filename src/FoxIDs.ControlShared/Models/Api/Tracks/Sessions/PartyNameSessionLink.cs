using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class PartyNameSessionLink
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        [Required]
        public PartyTypes Type { get; set; }
    }
}
