using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class SmsSettings
    {
        [Required]
        public SendSmsTypes Type { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.SendSms.FromNameLength)]
        public string FromName { get; set; }

        [MaxLength(Constants.Models.Track.SendSms.ClientIdLength)]
        public string ApiUrl { get; set; }

        [MaxLength(Constants.Models.Track.SendSms.ClientIdLength)]
        public string ClientId { get; set; }

        [MaxLength(Constants.Models.Track.SendSms.ClientSecretLength)]
        public string ClientSecret { get; set; }

        [MaxLength(Constants.Models.Track.SendSms.LabelLength)]
        public string Label { get; set; }

        public string CertificatePemCrt { get; set; }
        public string CertificatePemKey { get; set; }

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
                        results.Add(new ValidationResult($"The field {nameof(ClientId)} is required.", [nameof(ClientId)]));
                    }
                    if (ClientSecret.IsNullOrWhiteSpace())
                    {
                        results.Add(new ValidationResult($"The field {nameof(ClientSecret)} is required.", [nameof(ClientSecret)]));
                    }
                    if (CertificatePemCrt.IsNullOrWhiteSpace())
                    {
                        results.Add(new ValidationResult($"The field {nameof(CertificatePemCrt)} is required.", [nameof(CertificatePemCrt)]));
                    }
                    if (CertificatePemKey.IsNullOrWhiteSpace())
                    {
                        results.Add(new ValidationResult($"The field {nameof(CertificatePemKey)} is required.", [nameof(CertificatePemKey)]));
                    }
                    break;
                default:
                    //TODO add support for other email providers
                    throw new NotSupportedException();
            }

            return results;
        }
    }
}
