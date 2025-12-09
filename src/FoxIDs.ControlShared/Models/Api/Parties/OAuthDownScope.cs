using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Scope configuration including optional voluntary claims.
    /// </summary>
    public class OAuthDownScope
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
        /// Claims that may be returned when the scope is requested.
        /// </summary>
        [ListLength(Constants.Models.OAuthDownParty.Client.VoluntaryClaimsMin, Constants.Models.OAuthDownParty.Client.VoluntaryClaimsMax)]
        [Display(Name = "Voluntary claims")]
        public List<OAuthDownClaim> VoluntaryClaims { get; set; }
    }
}
