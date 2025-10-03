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

        /// <summary>
        /// Optional tenant filter. Defaults to the route tenant when omitted.
        /// </summary>
        public string TenantName { get; set; }

        /// <summary>
        /// Optional track filter. Defaults to the route track when omitted.
        /// </summary>
        public string TrackName { get; set; }

        /// <summary>
        /// Filter by audit type.
        /// </summary>
        public string AuditType { get; set; }

        /// <summary>
        /// Filter by audit data type.
        /// </summary>
        public string AuditDataType { get; set; }

        /// <summary>
        /// Filter by audit data action.
        /// </summary>
        public string AuditDataAction { get; set; }

        /// <summary>
        /// Filter by document identifier.
        /// </summary>
        public string DocumentId { get; set; }

        /// <summary>
        /// Filter by partition identifier.
        /// </summary>
        public string PartitionId { get; set; }

        /// <summary>
        /// Filter by audit data payload contents.
        /// </summary>
        public string Data { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (FromTime <= 0)
            {
                results.Add(new ValidationResult($"The field {nameof(FromTime)} is required.", new[] { nameof(FromTime) }));
            }
            if (ToTime <= 0)
            {
                results.Add(new ValidationResult($"The field {nameof(ToTime)} is required.", new[] { nameof(ToTime) }));
            }
            if (ToTime > 0 && FromTime > 0 && ToTime - FromTime > 86400)
            {
                results.Add(new ValidationResult($"The max time between {nameof(FromTime)} and {nameof(ToTime)} is 24 hours.", new[] { nameof(FromTime), nameof(ToTime) }));
            }
            if (ToTime > 0 && FromTime > 0 && ToTime < FromTime)
            {
                results.Add(new ValidationResult($"The value of {nameof(ToTime)} must be greater than or equal to {nameof(FromTime)}.", new[] { nameof(FromTime), nameof(ToTime) }));
            }

            return results;
        }
    }
}
