using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ExtendedUi : IValidatableObject
    {
        [Required]
        [MaxLength(Constants.Models.ExtendedUi.NameLength)]
        [RegularExpression(Constants.Models.ExtendedUi.NameRegExPattern)]
        [Display(Name = "Technical UI name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(Constants.Models.ExtendedUi.TitleLength)]
        [Display(Name = "Page title")]
        public string Title { get; set; }

        [MaxLength(Constants.Models.ExtendedUi.SubmitButtonTextLength)]
        [Display(Name = "Submit button text (default Log in)")]
        public string SubmitButtonText { get; set; }

        /// <summary>
        /// UI elements.
        /// </summary>
        [ListLength(Constants.Models.ExtendedUi.ElementsMin, Constants.Models.DynamicElements.ElementsMax)]
        public List<DynamicElement> Elements { get; set; }

        #region ExternalApi
        public ExternalConnectTypes ExternalConnectType { get; set; }

        [ListLength(Constants.Models.ExtendedUi.ExternalClaimsInMin, Constants.Models.ExtendedUi.ExternalClaimsInMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        public List<string> ExternalClaimsIn { get; set; }

        [MaxLength(Constants.Models.ExternalApi.ApiUrlLength)]
        [Display(Name = "API URL")]
        public string ApiUrl { get; set; }

        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [Display(Name = "API secret")]
        public string Secret { get; set; }

        /// <summary>
        /// API secret loaded. Used to compare loaded and updated value.
        /// </summary>
        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        public string SecretLoaded { get; set; }

        [MaxLength(Constants.Models.DynamicElements.ErrorMessageLength)]
        [Display(Name = "Generic error message")]
        public string ErrorMessage { get; set; }
        #endregion

        /// <summary>
        /// Run after successful completion of external UI.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (ExternalConnectType == ExternalConnectTypes.Api)
            {
                if (ApiUrl.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field '{nameof(ApiUrl)}' is required if the {nameof(ExternalConnectType)} is '{ExternalConnectType}'.", [nameof(ApiUrl), nameof(ExternalConnectType)]));
                }

                if (ErrorMessage.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field '{nameof(ErrorMessage)}' is required if the {nameof(ExternalConnectType)} is '{ExternalConnectType}'.", [nameof(ErrorMessage), nameof(ExternalConnectType)]));
                }
            }

            return results;
        }
    }
}
