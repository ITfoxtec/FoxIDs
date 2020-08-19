using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ClaimAndValues
    {
        [Required]
        [MaxLength(Constants.Models.Claim.ClaimsMapJwtClaimLength)]
        [Display(Name = "Claim")]
        public string Claim { get; set; }

        [Length(Constants.Models.Claim.ClaimValuesMin, Constants.Models.Claim.ClaimValuesMax, Constants.LengthDefinitions.JwtClaimValue)]
        [Display(Name = "Values")]
        public List<string> Values { get; set; }
    }
}