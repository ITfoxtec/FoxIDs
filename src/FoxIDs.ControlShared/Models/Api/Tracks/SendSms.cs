using ITfoxtec.Identity;
using ITfoxtec.Identity.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class SendSms : IValidatableObject
    {
        [Required]
        public SendSmsTypes Type { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.SendSms.FromNameLength)]
        public string FromName { get; set; }

        [MaxLength(Constants.Models.Track.SendSms.ApiUrlLength)]
        public string ApiUrl { get; set; }

        [MaxLength(Constants.Models.Track.SendSms.ClientIdLength)]
        public string ClientId { get; set; }

        [MaxLength(Constants.Models.Track.SendSms.ClientSecretLength)]
        public string ClientSecret { get; set; }

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
