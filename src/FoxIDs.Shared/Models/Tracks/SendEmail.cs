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
        /// Sendgrid API key.
        /// </summary>
        [MaxLength(Constants.Models.Track.SendEmail.SendgridApiKeyLength)]
        [JsonProperty(PropertyName = "sendgrid_api_key")]
        public string SendgridApiKey { get; set; }

        //TODO add support for other email providers

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (SendgridApiKey.IsNullOrEmpty())
            {
                results.Add(new ValidationResult($"At least one email providers is required. The field {nameof(SendgridApiKey)} is required.", new[] { nameof(SendgridApiKey) }));
            }
            //TODO add support for other email providers
            return results;
        }
    }
}
