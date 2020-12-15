using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ClaimAndValues
    {
        [Required]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeRegExPattern)]
        [Display(Name = "Claim")]
        public string Claim { get; set; }

        [Length(Constants.Models.Claim.ValuesUserMin, Constants.Models.Claim.ValuesMax, Constants.Models.Claim.ValueLength)]
        [Display(Name = "Values")]
        public List<string> Values { get; set; }
    }
}