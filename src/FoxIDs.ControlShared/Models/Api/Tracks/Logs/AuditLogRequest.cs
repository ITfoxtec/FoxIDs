using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Request payload for querying audit logs.
    /// </summary>
    public class AuditLogRequest : IValidatableObject
    {
        /// <summary>
        /// Audit log request from time in Unix time seconds.
        /// </summary>
        [Required]
        public long FromTime { get; set; }

        /// <summary>
        /// Audit log request to time in Unix time seconds.
        /// </summary>
        [Required]
        public long ToTime { get; set; }

        /// <summary>
        /// General free-text filter applied across audit fields and data payload.
        /// </summary>
        public string Filter { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (FromTime <= 0)
            {
                results.Add(new ValidationResult($"The field {nameof(FromTime)} is required.", [nameof(FromTime)]));
            }
            if (ToTime <= 0)
            {
                results.Add(new ValidationResult($"The field {nameof(ToTime)} is required.", [nameof(ToTime)]));
            }
            if (ToTime > 0 && FromTime > 0 && ToTime - FromTime > 86400)
            {
                results.Add(new ValidationResult($"The max time between {nameof(FromTime)} and {nameof(ToTime)} is 24 hours.", [nameof(FromTime), nameof(ToTime)]));
            }
            if (ToTime > 0 && FromTime > 0 && ToTime < FromTime)
            {
                results.Add(new ValidationResult($"The value of {nameof(ToTime)} must be greater than or equal to {nameof(FromTime)}.", [nameof(FromTime), nameof(ToTime)]));
            }

            return results;
        }
    }
}
