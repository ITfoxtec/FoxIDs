using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ClaimAndValues
    {
        [Required]
        [MaxLength(Constants.Models.Claim.ClaimsMapJwtClaimLength)]
        [JsonProperty(PropertyName = "claim")]
        public string Claim { get; set; }

        [Length(Constants.Models.Claim.ClaimValuesMin, Constants.Models.Claim.ClaimValuesMax, Constants.LengthDefinitions.JwtClaimValue)]
        [JsonProperty(PropertyName = "values")]
        public List<string> Values { get; set; }
    }
}
