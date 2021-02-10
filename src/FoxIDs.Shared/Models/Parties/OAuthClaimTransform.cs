using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class OAuthClaimTransform : ClaimTransform
    {
        [Length(Constants.Models.Claim.TransformClaimsInMin, Constants.Models.Claim.TransformClaimsInMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeRegExPattern)]
        [JsonProperty(PropertyName = "claims_in")]
        public override List<string> ClaimsIn { get; set; }

        [Required]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeRegExPattern)]
        [JsonProperty(PropertyName = "claim_out")]
        public override string ClaimOut { get; set; }

        [Required]
        [MaxLength(Constants.Models.Claim.TransformTransformationLength)]
        [JsonProperty(PropertyName = "transformation")]
        public override string Transformation { get; set; }

        [MaxLength(Constants.Models.Claim.TransformTransformationLength)]
        [JsonProperty(PropertyName = "transformation_extension")]
        public override string TransformationExtension { get; set; }
    }
}
