using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class ExternalPasswordViewModel : IValidatableObject
    {
        [Display(Name = "Validate on login")]
        public bool EnabledValidationCurrent { get; set; }

        [Display(Name = "Validate on password change")]
        public bool EnabledValidationNew { get; set; }

        [Display(Name = "Notify on login")]
        public bool EnabledNotificationCurrent { get; set; }

        [Display(Name = "Notify on password change")]
        public bool EnabledNotificationNew { get; set; }

        public ExternalConnectTypes ExternalConnectType { get; set; } = ExternalConnectTypes.Api;

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

            if (EnabledValidationCurrent || EnabledValidationNew || EnabledNotificationCurrent || EnabledNotificationNew)
            {
                if (ApiUrl.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field '{nameof(ApiUrl)}' is required to enable validation or notification.", [nameof(ApiUrl)]));
                }
                if (Secret.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field '{nameof(Secret)}' is required to enable validation or notification.", [nameof(Secret)]));
                }
            }

            if (!ApiUrl.IsNullOrWhiteSpace() && Secret.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field '{nameof(Secret)}' is required.", [nameof(Secret)]));
            }

            return results;
        }
    }
}
