using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class OAuthDownClaim
    {
        [Required]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [JsonProperty(PropertyName = "claim")]
        public string Claim { get; set; }

        [ListLength(Constants.Models.Claim.ValuesOAuthMin, Constants.Models.Claim.ValuesMax, Constants.Models.Claim.ValueLength, Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "values")]
        public List<string> Values { get; set; }
    }
}
