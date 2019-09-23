using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class OidcDownScope
    {
        [Required]
        [MaxLength(Constants.Models.OAuthParty.ScopesLength)]
        [RegularExpression(Constants.Models.OAuthParty.ScopeRegExPattern)]
        public string Scope { get; set; }

        [Length(Constants.Models.OAuthParty.Client.VoluntaryClaimsMin, Constants.Models.OAuthParty.Client.VoluntaryClaimsMax)]
        public List<OidcDownClaim> VoluntaryClaims { get; set; }
    }
}
