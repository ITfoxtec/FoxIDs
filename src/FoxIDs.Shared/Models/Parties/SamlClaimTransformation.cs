using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class SamlClaimTransformation : ClaimTransformation
    {
        [Length(Constants.Models.Party.ClaimTransformationClaimsMin, Constants.Models.Party.ClaimTransformationClaimsMax, Constants.Models.SamlParty.ClaimsLength)]
        [JsonProperty(PropertyName = "claims_in")]
        public override List<string> ClaimsIn { get; set; }

        [Required]
        [MaxLength(Constants.Models.SamlParty.ClaimsLength)]
        [JsonProperty(PropertyName = "claim_out")]
        public override string ClaimOut { get; set; }

        [Required]
        [MaxLength(Constants.Models.SamlParty.ClaimsLength)]
        [JsonProperty(PropertyName = "transformation")]
        public override string Transformation { get; set; }
    }
}
