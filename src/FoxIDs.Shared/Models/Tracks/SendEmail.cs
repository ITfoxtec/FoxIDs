using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class SendEmail : IValidatableObject
    {
        /// <summary>
        /// From email.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        [JsonProperty(PropertyName = "frome_mail")]
        public string FromEmail { get; set; }

        /// <summary>
        /// From name (name associated to the email address).
        /// </summary>
        [MaxLength(Constants.Models.Track.SendEmail.FromNameLength)]
        [JsonProperty(PropertyName = "from_name")]
        public string FromName { get; set; }

        /// <summary>
        /// Sendgrid API key.
        /// </summary>
        [MaxLength(Constants.Models.Track.SendEmail.SendgridApiKeyLength)]
        [JsonProperty(PropertyName = "sendgrid_api_key")]
        public string SendgridApiKey { get; set; }

        /// <summary>
        /// SMTP host.
        /// </summary>
        [MaxLength(Constants.Models.Track.SendEmail.SmtpHostLength)]
        [JsonProperty(PropertyName = "smtp_host")]
        public string SmtpHost { get; set; }

        /// <summary>
        /// SMTP port.
        /// </summary>
        [JsonProperty(PropertyName = "smtp_port")]
        public int SmtpPort { get; set; }

        /// <summary>
        /// SMTP username.
        /// </summary>
        [MaxLength(Constants.Models.Track.SendEmail.SmtpUsernameLength)]
        [JsonProperty(PropertyName = "smtp_username")]
        public string SmtpUsername { get; set; }

        /// <summary>
        /// SMTP password.
        /// </summary>
        [MaxLength(Constants.Models.Track.SendEmail.SmtpPasswordLength)]
        [JsonProperty(PropertyName = "smtp_password")]
        public string SmtpPassword { get; set; }

        //TODO add support for other email providers

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            var hasProvider = false;
            if (!SendgridApiKey.IsNullOrWhiteSpace())
            {
                hasProvider = true;
            }

            if (!SmtpHost.IsNullOrWhiteSpace() && SmtpPort > 0 && !SmtpUsername.IsNullOrWhiteSpace() && !SmtpPassword.IsNullOrWhiteSpace())
            {
                hasProvider = true;
            }

            //TODO add support for other email providers

            if (!hasProvider)
            {
                results.Add(new ValidationResult($"At least one email providers is required. The field {nameof(SendgridApiKey)} or SMTP fields is required.",
                    new[] { nameof(SendgridApiKey), nameof(SmtpHost), nameof(SmtpPort), nameof(SmtpUsername), nameof(SmtpPassword) }));

            }
            return results;
        }
    }
}
