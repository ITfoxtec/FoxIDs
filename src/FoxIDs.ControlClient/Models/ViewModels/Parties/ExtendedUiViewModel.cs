using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class ExtendedUiViewModel : ExtendedUi, IDynamicElementsViewModel
    {
        [ValidateComplexType]
        [ListLength(Constants.Models.ExtendedUi.ElementsMin, Constants.Models.DynamicElements.ElementsMax)]
        public new List<DynamicElementViewModel> Elements { get; set; } = new List<DynamicElementViewModel>();

        [ValidateComplexType]
        [ListLength(Constants.Models.ExtendedUi.ExternalClaimsInMin, Constants.Models.ExtendedUi.ExternalClaimsInMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [Display(Name = "Select which claims should be included in the API request (user * to select all claims)")]
        public new List<string> ExternalClaimsIn { get; set; }

        /// <summary>
        /// Run after successful completion of external UI.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public new List<ClaimTransformViewModel> ClaimTransforms { get; set; } = new List<ClaimTransformViewModel>();

        public bool ShowDetails { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (!ApiUrl.IsNullOrWhiteSpace())
            {
                if (Secret.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The secret is required for API validation.", [nameof(Secret)]));
                }

                if (ErrorMessage.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The generic error message is required for API validation.", [nameof(ErrorMessage)]));
                }
            }

            return results;
        }
    }
}
