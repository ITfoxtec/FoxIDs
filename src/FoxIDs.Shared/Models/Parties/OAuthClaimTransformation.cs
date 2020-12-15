using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class OAuthClaimTransformation : ClaimTransformation
    {
        [Length(Constants.Models.Party.ClaimTransformationClaimsMin, Constants.Models.Party.ClaimTransformationClaimsMax, Constants.Models.Claim.JwtTypeLength)]
        [JsonProperty(PropertyName = "claims_in")]
        public override List<string> ClaimsIn { get; set; }

        [Required]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [JsonProperty(PropertyName = "claim_out")]
        public override string ClaimOut { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.ClaimTransformationLength)]
        [JsonProperty(PropertyName = "transformation")]
        public override string Transformation { get; set; }

        [MaxLength(Constants.Models.Party.ClaimTransformationLength)]
        [JsonProperty(PropertyName = "transformation_extension")]
        public override string TransformationExtension { get; set; }
    }
}
