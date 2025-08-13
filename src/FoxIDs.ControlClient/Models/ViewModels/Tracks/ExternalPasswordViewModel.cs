using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class ExternalPasswordViewModel : IValidatableObject
    {
        [Display(Name = "Enabled validation")]
        public bool EnabledValidation { get; set; }

        [Display(Name = "Enabled notification")]
        public bool EnabledNotification { get; set; }

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

            if (EnabledValidation || EnabledNotification)
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
