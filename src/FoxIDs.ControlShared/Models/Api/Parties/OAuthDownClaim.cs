using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Claim definition issued by an OAuth down-party.
    /// </summary>
    public class OAuthDownClaim
    {
        /// <summary>
        /// Claim type to issue.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [Display(Name = "Claim")]
        public string Claim { get; set; }

        /// <summary>
        /// Allowed claim values.
        /// </summary>
        [ListLength(Constants.Models.Claim.ValuesOAuthMin, Constants.Models.Claim.ValuesMax, Constants.Models.Claim.ValueLength, Constants.Models.Claim.ValueLength)]
        [Display(Name = "Values")]
        public List<string> Values { get; set; }
    }
}
