using FoxIDs.Infrastructure.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models.Api
{
    public class SamlClaimTransform : ClaimTransform
    {
        [ListLength(Constants.Models.Claim.TransformClaimsInMin, Constants.Models.Claim.TransformClaimsInMax, Constants.Models.Claim.SamlTypeLength, Constants.Models.Claim.SamlTypeWildcardRegExPattern)]
        public override List<string> ClaimsIn { get; set; }

        [MaxLength(Constants.Models.Claim.SamlTypeLength)]
        [RegularExpression(Constants.Models.Claim.SamlTypeWildcardRegExPattern)]
        [Obsolete($"Use {nameof(ClaimsIn)} instead. Delete after 2028-07-01.")]
        public override string ClaimOut
        {
            get
            {
                return ClaimsOut?.First();
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && !(ClaimsOut?.Count > 0))
                {
                    ClaimsOut = new List<string> { value };
                }
            }
        }

        [ListLength(Constants.Models.Claim.TransformClaimsOutMin, Constants.Models.Claim.TransformClaimsOutMax, Constants.Models.Claim.SamlTypeLength, Constants.Models.Claim.SamlTypeWildcardRegExPattern)]
        public override List<string> ClaimsOut { get; set; }

        [MaxLength(Constants.Models.Claim.TransformTransformationLength)]
        public override string Transformation { get; set; }

        [MaxLength(Constants.Models.Claim.TransformTransformationLength)]
        public override string TransformationExtension { get; set; }
    }
}
