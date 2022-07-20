using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class FoxIDsControlSettings : Settings, IValidatableObject
    {
        [Required]
        public string DownParty { get; set; }

        [Required]
        public ApplicationInsightsSettings ApplicationInsights { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (FoxIDsEndpoint.IsNullOrEmpty())
            {
                results.Add(new ValidationResult($"The field {nameof(FoxIDsEndpoint)} is required.", new[] { nameof(FoxIDsEndpoint) }));
            }
            if (FoxIDsControlEndpoint.IsNullOrEmpty())
            {
                results.Add(new ValidationResult($"The field {nameof(FoxIDsControlEndpoint)} is required.", new[] { nameof(FoxIDsControlEndpoint) }));
            }
            return results;
        }
    }
}
