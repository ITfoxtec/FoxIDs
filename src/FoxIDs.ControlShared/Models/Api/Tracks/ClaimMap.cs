using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Map between JWT and SAML claim type.
    /// </summary>
    public class ClaimMap
    {
        /// <summary>
        /// JWT claim type.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeRegExPattern)]
        [Display(Name = "JWT claim")]
        public string JwtClaim { get; set; }

        /// <summary>
        /// SAML claim type.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Claim.SamlTypeLength)]
        [RegularExpression(Constants.Models.Claim.SamlTypeRegExPattern)]
        [Display(Name = "SAML claim")]
        public string SamlClaim { get; set; }
    }
}
