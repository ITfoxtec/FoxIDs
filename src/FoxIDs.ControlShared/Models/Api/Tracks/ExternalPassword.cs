using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ExternalPassword : IValidatableObject
    {
        [Display(Name = "Enabled validation")]
        public bool EnabledValidation { get; set; }

        [Display(Name = "Enabled notification")]
        public bool EnabledNotification { get; set; }

        [Display(Name = "External connect type")]
        public ExternalConnectTypes ExternalConnectType { get; set; }

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

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

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
            }

            return results;
        }
    }
}
