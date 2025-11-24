using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models
{
    public class OAuthClaimTransform : ClaimTransform
    {
        [ListLength(Constants.Models.Claim.TransformClaimsInMin, Constants.Models.Claim.TransformClaimsInMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [JsonProperty(PropertyName = "claims_in")]
        public override List<string> ClaimsIn { get; set; }

        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [JsonProperty(PropertyName = "claim_out")]
        [Obsolete($"Use {nameof(ClaimsIn)} instead. Delete after 2028-07-01.")]
        public override string ClaimOut
        {
            get
            {
                return null;
            }
            set
            {
                if (!value.IsNullOrWhiteSpace() && !(ClaimsOut?.Count() > 0))
                {
                    ClaimsOut = [value];
                }
            }
        }

        [ListLength(Constants.Models.Claim.TransformClaimsOutMin, Constants.Models.Claim.TransformClaimsOutMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [JsonProperty(PropertyName = "claims_out")]
        public override List<string> ClaimsOut { get; set; }

        [MaxLength(Constants.Models.Claim.TransformTransformationLength)]
        [JsonProperty(PropertyName = "transformation")]
        public override string Transformation { get; set; }

        [MaxLength(Constants.Models.Claim.TransformTransformationLength)]
        [JsonProperty(PropertyName = "transformation_extension")]
        public override string TransformationExtension { get; set; }
    }
}
