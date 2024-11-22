using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Client.Models.ViewModels
{
    public class SamlClaimTransformClaimsInViewModel : ClaimTransformViewModel
    {
        [ListLength(Constants.Models.Claim.TransformClaimsInMin, Constants.Models.Claim.TransformClaimsInMax, Constants.Models.Claim.SamlTypeLength, Constants.Models.Claim.SamlTypeRegExPattern)]
        [Display(Name = "Select claim")]
        public override List<string> ClaimsIn { get; set; }

        [MaxLength(Constants.Models.Claim.SamlTypeLength)]
        [RegularExpression(Constants.Models.Claim.SamlTypeRegExPattern)]
        public override string ClaimOut { get; set; }

        [MaxLength(Constants.Models.Claim.TransformTransformationLength)]
        public override string Transformation { get; set; }

        [MaxLength(Constants.Models.Claim.TransformTransformationLength)]
        public override string TransformationExtension { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Action == ClaimTransformActions.Add || Action == ClaimTransformActions.Replace)
            {
                if (ClaimsIn?.Count() < 1)
                {
                    results.Add(new ValidationResult($"At least one is required.", [nameof(ClaimsIn)]));
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
