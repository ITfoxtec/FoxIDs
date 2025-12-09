using FoxIDs.Infrastructure.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Claim transform configuration applied to OAuth/OIDC down-parties.
    /// </summary>
    public class OAuthClaimTransform : ClaimTransform
    {
        [ListLength(Constants.Models.Claim.TransformClaimsInMin, Constants.Models.Claim.TransformClaimsInMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        public override List<string> ClaimsIn { get; set; }

        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
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

        [ListLength(Constants.Models.Claim.TransformClaimsOutMin, Constants.Models.Claim.TransformClaimsOutMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        public override List<string> ClaimsOut { get; set; }

        [MaxLength(Constants.Models.Claim.TransformTransformationLength)]
        public override string Transformation { get; set; }

        [MaxLength(Constants.Models.Claim.TransformTransformationLength)]
        public override string TransformationExtension { get; set; }
    }
}
