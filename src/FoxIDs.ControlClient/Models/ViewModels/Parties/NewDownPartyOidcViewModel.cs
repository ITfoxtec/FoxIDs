using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class NewDownPartyOidcViewModel
    {
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Client ID / Resource name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        [Display(Name = "Name")]
        public string DisplayName { get; set; }

        [ListLength(Constants.Models.OidcDownParty.Client.RedirectUrisMin, Constants.Models.OAuthDownParty.Client.RedirectUrisMax, Constants.Models.OAuthDownParty.Client.RedirectUriLength)]
        [Display(Name = "Redirect URIs")]
        public List<string> RedirectUris { get; set; }

        [Display(Name = "Absolute URIs")]
        public bool DisableAbsoluteUris { get; set; } = true;
       
        [Display(Name = "Authority")]
        public string Authority { get; set; }

        [Display(Name = "Client secret (can only be viewed immediately after creation)")]
        public string Secret { get; set; } 
        
        [Display(Name = "Proof Key for Code Exchange (PKCE)")]
        public string Pkce { get; set; }

        [Display(Name = "Scopes")]
        public List<string> Scopes { get; set; }
    }
}
