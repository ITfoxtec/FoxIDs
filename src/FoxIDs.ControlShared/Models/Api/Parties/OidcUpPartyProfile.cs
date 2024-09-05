using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class OidcUpPartyProfile 
    {
        [Required]
        [MaxLength(Constants.Models.Party.ProfileNameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        public string DisplayName { get; set; }

        [Required]
        public OidcUpClientProfile Client { get; set; }
    }
}
