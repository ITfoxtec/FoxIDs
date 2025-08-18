using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ExternalPassword : IValidatableObject
    {
        [JsonProperty(PropertyName = "enabled_validation")]
        public bool EnabledValidation { get; set; }

        [JsonProperty(PropertyName = "enabled_notification")]
        public bool EnabledNotification { get; set; }

        [Display(Name = "External connect type")]
        public ExternalConnectTypes ExternalConnectType { get; set; }

        [MaxLength(Constants.Models.ExternalApi.ApiUrlLength)]
        [JsonProperty(PropertyName = "api_url")]
        public string ApiUrl { get; set; }

        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [JsonProperty(PropertyName = "secret")]
        public string Secret { get; set; }

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
