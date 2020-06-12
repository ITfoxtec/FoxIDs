using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class OAuthDownScope
    {
        [Required]
        [MaxLength(Constants.Models.OAuthDownParty.ScopesLength)]
        [RegularExpression(Constants.Models.OAuthDownParty.ScopeRegExPattern)]
        public string Scope { get; set; }

        [Length(Constants.Models.OAuthDownParty.Client.VoluntaryClaimsMin, Constants.Models.OAuthDownParty.Client.VoluntaryClaimsMax)]
        public List<OAuthDownClaim> VoluntaryClaims { get; set; }
    }
}
