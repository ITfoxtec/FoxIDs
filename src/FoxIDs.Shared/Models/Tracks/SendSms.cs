using ITfoxtec.Identity;
using ITfoxtec.Identity.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class SendSms : IValidatableObject
    {
        [Required]
        [JsonProperty(PropertyName = "type")]
        public SendSmsTypes Type { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.SendSms.FromNameLength)]
        [JsonProperty(PropertyName = "from_name")]
        public string FromName { get; set; }

        [MaxLength(Constants.Models.Track.SendSms.ApiUrlLength)]
        [JsonProperty(PropertyName = "api_url")]
        public string ApiUrl { get; set; }

        [MaxLength(Constants.Models.Track.SendSms.ClientIdLength)]
        [JsonProperty(PropertyName = "client_id")]
        public string ClientId { get; set; }

        [MaxLength(Constants.Models.Track.SendSms.ClientSecretLength)]
        [JsonProperty(PropertyName = "client_secret")]
        public string ClientSecret { get; set; }

        [MaxLength(Constants.Models.Track.SendSms.LabelLength)]
        [JsonProperty(PropertyName = "label")]
        public string Label { get; set; }

        [JsonProperty(PropertyName = "key")]
        public JsonWebKey Key { get; set; }

        //TODO add support for other SMS providers

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            switch (Type)
            {
                case SendSmsTypes.GatewayApi:
                    if (ApiUrl.IsNullOrWhiteSpace())
                    {
                        results.Add(new ValidationResult($"The field {nameof(ApiUrl)} is required.", [nameof(ApiUrl)]));
                    }
                    if (ClientSecret.IsNullOrWhiteSpace())
                    {
                        results.Add(new ValidationResult($"The field {nameof(ClientSecret)} is required.", [nameof(ClientSecret)]));
                    }
                    break;
                case SendSmsTypes.Smstools:
                    if (ApiUrl.IsNullOrWhiteSpace())
                    {
                        results.Add(new ValidationResult($"The field {nameof(ApiUrl)} is required.", [nameof(ApiUrl)]));
                    }
                    if (ClientId.IsNullOrWhiteSpace())
                    {
                        results.Add(new ValidationResult($"The field {nameof(ClientId)} is required.", [nameof(ClientId)]));
                    }
                    if (ClientSecret.IsNullOrWhiteSpace())
                    {
                        results.Add(new ValidationResult($"The field {nameof(ClientSecret)} is required.", [nameof(ClientSecret)]));
                    }
                    break;
                case SendSmsTypes.TeliaSmsGateway:
                    if (ApiUrl.IsNullOrWhiteSpace())
                    {
                        results.Add(new ValidationResult($"The field {nameof(ApiUrl)} is required.", [nameof(ApiUrl)]));
                    }
                    if (ClientId.IsNullOrWhiteSpace())
                    {
                        results.Add(new ValidationResult($"The field {nameof(ClientId)} (sender address) is required.", [nameof(ClientId)]));
                    }
                    if (ClientSecret.IsNullOrWhiteSpace())
                    {
                        results.Add(new ValidationResult($"The field {nameof(ClientSecret)} (API key) is required.", [nameof(ClientSecret)]));
                    }
                    if (Key == null)
                    {
                        results.Add(new ValidationResult($"The field {nameof(Key)} (mTLS certificate) is required.", [nameof(Key)]));
                    }
                    break;

                //TODO add support for other email providers

                default:
                    break;
            }

            return results;
        }
    }
}
