using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class SamlClaimTransform : ClaimTransform
    {
        [ListLength(Constants.Models.Claim.TransformClaimsInMin, Constants.Models.Claim.TransformClaimsInMax, Constants.Models.Claim.SamlTypeLength, Constants.Models.Claim.SamlTypeWildcardRegExPattern)]
        public override List<string> ClaimsIn { get; set; }

        [MaxLength(Constants.Models.Claim.SamlTypeLength)]
        [RegularExpression(Constants.Models.Claim.SamlTypeRegExPattern)]
        public override string ClaimOut { get; set; }

        [MaxLength(Constants.Models.Claim.TransformTransformationLength)]
        public override string Transformation { get; set; }

        [MaxLength(Constants.Models.Claim.TransformTransformationLength)]
        public override string TransformationExtension { get; set; }
    }
}
