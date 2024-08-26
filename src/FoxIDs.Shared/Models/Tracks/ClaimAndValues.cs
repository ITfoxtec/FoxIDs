using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ClaimAndValues
    {
        [Required]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeRegExPattern)]
        [JsonProperty(PropertyName = "claim")]
        public string Claim { get; set; }

        [ListLength(Constants.Models.Claim.ValuesUserMin, Constants.Models.Claim.ProcessValuesMax, Constants.Models.Claim.ProcessValueLength, Constants.Models.Claim.ProcessValueLength)]
        [JsonProperty(PropertyName = "values")]
        public List<string> Values { get; set; }
    }
}
