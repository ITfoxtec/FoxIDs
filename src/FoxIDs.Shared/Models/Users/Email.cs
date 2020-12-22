using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class Email : IValidatableObject
    {
        /// <summary>
        /// From email.
        /// </summary>
        [Required]
        public string FromEmail { get; set; }

        /// <summary>
        /// Sendgrid API key.
        /// </summary>
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
