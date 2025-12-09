using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Claim name with one or more associated values.
    /// </summary>
    public class ClaimAndValues
    {
        /// <summary>
        /// Claim type (JWT name).
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeRegExPattern)]
        [Display(Name = "Claim")]
        public string Claim { get; set; }

        /// <summary>
        /// Claim values issued to the user.
        /// </summary>
        [ListLength(Constants.Models.Claim.ValuesUserMin, Constants.Models.Claim.ValuesMax, Constants.Models.Claim.ValueLength, Constants.Models.Claim.ValueLength)]
        [Display(Name = "Values")]
        public List<string> Values { get; set; }
    }
}