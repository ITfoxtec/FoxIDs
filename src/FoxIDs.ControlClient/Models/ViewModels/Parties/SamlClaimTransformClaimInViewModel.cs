using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Client.Models.ViewModels
{
    public class SamlClaimTransformClaimInViewModel : SamlClaimTransformViewModel, IValidatableObject
    {
        [MaxLength(Constants.Models.Claim.SamlTypeLength)]
        [RegularExpression(Constants.Models.Claim.SamlTypeRegExPattern)]
        [Display(Name = "Select claim")]
        public string ClaimIn { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Action != ClaimTransformActions.Remove)
            {
                if (ClaimIn.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field Select claim is required.", new[] { nameof(Transformation) }));
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
