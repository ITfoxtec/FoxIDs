using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    // Used to query logs.
    public class LogRequest : IValidatableObject
    {
        /// <summary>
        /// Log request from time in Unix time seconds. E.g. DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        /// </summary>
        [Required]
        public long FromTime { get; set; }

        /// <summary>
        /// Log request from time in Unix time seconds. E.g. DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds()
        /// </summary>
        [Required]
        public long ToTime { get; set; } 

        public string Filter { get; set; }

        // For backward compatibility.
        public bool QueryExceptions { get; set; }

        public bool QueryErrors { get; set; }

        public bool QueryWarnings { get; set; }

        public bool QueryTraces { get; set; }

        public bool QueryEvents { get; set; }

        public bool QueryMetrics { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (FromTime <= 0)
            {
                results.Add(new ValidationResult($"The field {nameof(FromTime)} is required.", new[] { nameof(FromTime) }));
            }
            if (ToTime <= 0)
            {
                results.Add(new ValidationResult($"The field {nameof(FromTime)} is required.", new[] { nameof(FromTime) }));
            }
            if (ToTime - FromTime > 86400) // max 24 hours
            {
                results.Add(new ValidationResult($"The max time between {nameof(FromTime)} and {nameof(ToTime)} is 24 hours.", new[] { nameof(FromTime) }));
            }
            return results;
        }
    }
}
