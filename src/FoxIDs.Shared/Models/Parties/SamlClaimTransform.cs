using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class SamlClaimTransform : ClaimTransform
    {
        [Length(Constants.Models.Claim.TransformClaimsInMin, Constants.Models.Claim.TransformClaimsInMax, Constants.Models.Claim.SamlTypeLength, Constants.Models.Claim.SamlTypeRegExPattern)]
        [JsonProperty(PropertyName = "claims_in")]
        public override List<string> ClaimsIn { get; set; }

        [Required]
        [MaxLength(Constants.Models.Claim.SamlTypeLength)]
        [RegularExpression(Constants.Models.Claim.SamlTypeRegExPattern)]
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
