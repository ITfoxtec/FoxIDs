using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class OAuthDownResourceScope
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [Display(Name = "Resource")]
        public string Resource { get; set; }

        [Length(Constants.Models.OAuthDownParty.Client.ScopesMin, Constants.Models.OAuthDownParty.Client.ScopesMax, Constants.Models.OAuthDownParty.ScopeLength, Constants.Models.OAuthDownParty.ScopeRegExPattern)]
        [Display(Name = "Scopes")]
        public List<string> Scopes { get; set; }
    }
}
