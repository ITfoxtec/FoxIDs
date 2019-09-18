using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class OAuthDownScope
    {
        [Required]
        [MaxLength(Constants.Models.OAuthParty.ScopesLength)]
        [RegularExpression(Constants.Models.OAuthParty.ScopeRegExPattern)]
        public string Scope { get; set; }

        [Length(Constants.Models.OAuthParty.Client.VoluntaryClaimsMin, Constants.Models.OAuthParty.Client.VoluntaryClaimsMax)]
        public List<OAuthDownClaim> VoluntaryClaims { get; set; }
    }
}
