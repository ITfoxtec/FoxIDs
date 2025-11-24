using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OAuthClaimTransformClaimInClaimsOutViewModel : ClaimTransformViewModel
    {
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [Display(Name = "Select claim")]
        public override string ClaimIn { get; set; }

        [ListLength(Constants.Models.Claim.TransformClaimsOutMin, Constants.Models.Claim.TransformClaimsOutMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        public override List<string> ClaimsOut { get; set; }

        [MaxLength(Constants.Models.Claim.TransformTransformationLength)]
        public override string Transformation { get; set; }

        [MaxLength(Constants.Models.Claim.TransformTransformationLength)]
        public override string TransformationExtension { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Type != ClaimTransformTypes.Constant && Action != ClaimTransformActions.Remove)
            {
                if (ClaimIn.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field is required.", [nameof(ClaimIn)]));
                }
            }

            var baseResults = base.Validate(validationContext);
            if (baseResults.Count() > 0)
            {
                results.AddRange(baseResults);
            }
            return results;
        }
    }
}
