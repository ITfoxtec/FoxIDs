using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// OIDC scope definition with voluntary claims.
    /// </summary>
    public class OidcDownScope
    {
        /// <summary>
        /// Scope name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.OAuthDownParty.ScopeLength)]
        [RegularExpression(Constants.Models.OAuthDownParty.ScopeRegExPattern)]
        [Display(Name = "Scope")]
        public string Scope { get; set; }

        /// <summary>
        /// Claims that may be returned for the scope.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.OAuthDownParty.Client.VoluntaryClaimsMin, Constants.Models.OAuthDownParty.Client.VoluntaryClaimsMax)]
        [Display(Name = "Voluntary claims")]
        public List<OidcDownClaim> VoluntaryClaims { get; set; }
    }
}
