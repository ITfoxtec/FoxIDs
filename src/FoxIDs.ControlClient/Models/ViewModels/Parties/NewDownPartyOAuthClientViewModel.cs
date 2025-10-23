using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class NewDownPartyOAuthClientViewModel
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Client ID")]
        public string Name { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        [Display(Name = "Name")]
        public string DisplayName { get; set; }
      
        [Display(Name = "Authority")]
        public string Authority { get; set; }

        [Display(Name = "Client secret")]
        public string Secret { get; set; }

        [Display(Name = "OIDC Discovery")]
        public string OidcDiscovery { get; set; }

        [Display(Name = "Token URL")]
        public string TokenUrl { get; set; }
    }
}
