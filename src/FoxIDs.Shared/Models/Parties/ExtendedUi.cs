using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ExtendedUi : IOAuthClaimTransformsRef, IValidatableObject
    {
        [Required]
        [MaxLength(Constants.Models.ExtendedUi.NameLength)]
        [RegularExpression(Constants.Models.ExtendedUi.NameRegExPattern)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.ExtendedUi.TitleLength)]
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [MaxLength(Constants.Models.ExtendedUi.SubmitButtonTextLength)]
        [JsonProperty(PropertyName = "submit_button")]
        public string SubmitButtonText { get; set; }

        [JsonProperty(PropertyName = "predef_type")]
        public ExtendedUiPredefinedTypes? PredefinedType { get; set; }

        [ValidateComplexType]
        [JsonProperty(PropertyName = "modules")]
        public ExtendedUiModules Modules { get; set; }

        /// <summary>
        /// UI elements.
        /// </summary>
        [ListLength(Constants.Models.DynamicElements.ElementsMin, Constants.Models.DynamicElements.ElementsMax)]
        [JsonProperty(PropertyName = "elements")]
        public List<DynamicElement> Elements { get; set; }

        #region ExternalApi
        [JsonProperty(PropertyName = "ext_con_type")]
        public ExternalConnectTypes? ExternalConnectType { get; set; }

        [ListLength(Constants.Models.ExtendedUi.ExternalClaimsInMin, Constants.Models.ExtendedUi.ExternalClaimsInMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [JsonProperty(PropertyName = "ext_claims_in")]
        public List<string> ExternalClaimsIn { get; set; }

        [MaxLength(Constants.Models.ExternalApi.ApiUrlLength)]
        [JsonProperty(PropertyName = "api_url")]
        public string ApiUrl { get; set; }

        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [JsonProperty(PropertyName = "secret")]
        public string Secret { get; set; }

        [MaxLength(Constants.Models.DynamicElements.ErrorMessageLength)]
        [JsonProperty(PropertyName = "error_message")]
        public string ErrorMessage { get; set; }
        #endregion

        /// <summary>
        /// Run after successful completion of external UI.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [JsonProperty(PropertyName = "claim_transforms")]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (PredefinedType == null)
            {
                if (Title.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field '{nameof(Title)}' is required.", [nameof(Title)]));
                }

                if (Elements == null || Elements.Count < Constants.Models.ExtendedUi.ElementsMin)
                {
                    results.Add(new ValidationResult($"The field '{nameof(Elements)}' is required.", [nameof(Elements)]));
                }
            }
            else
            {
                Title = null;
                SubmitButtonText = null;
                Elements = null;

                if (PredefinedType == ExtendedUiPredefinedTypes.NemLoginPrivateCprMatch)
                {
                    if (Modules?.NemLogin == null)
                    {
                        results.Add(new ValidationResult($"The field '{nameof(Modules.NemLogin)}' is required when the predefined type is '{PredefinedType}'.", [$"{nameof(Modules)}.{nameof(Modules.NemLogin)}"]));
                    }
                }
                else
                {
                    results.Add(new ValidationResult($"The predefined type '{PredefinedType}' is not supported.", [nameof(PredefinedType)]));
                }
            }

            if (ExternalConnectType == ExternalConnectTypes.Api)
            {
                if (ApiUrl.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field '{nameof(ApiUrl)}' is required if the {nameof(ExternalConnectType)} is '{ExternalConnectType}'.", [nameof(ApiUrl), nameof(ExternalConnectType)]));
                }

                if (Secret.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field '{nameof(Secret)}' is required if the {nameof(ExternalConnectType)} is '{ExternalConnectType}'.", [nameof(Secret), nameof(ExternalConnectType)]));
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
