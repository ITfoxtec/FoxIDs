using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Request model for retrieving or calculating usage in a period.
    /// </summary>
    public class UsageRequest : IValidatableObject
    {
        /// <summary>
        /// Tenant name to fetch usage for.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameDbRegExPattern)]
        public string TenantName { get; set; }

        /// <summary>
        /// Start date of the requested period.
        /// </summary>
        [Required]
        public DateOnly PeriodBeginDate { get; set; }

        /// <summary>
        /// Optional end date; defaults to the same month if omitted.
        /// </summary>
        public DateOnly? PeriodEndDate { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (PeriodEndDate.HasValue)
            {
                if (PeriodBeginDate.Year != PeriodEndDate.Value.Year || PeriodBeginDate.Month != PeriodEndDate.Value.Month)
                {
                    results.Add(new ValidationResult($"The {nameof(PeriodBeginDate)} and {nameof(PeriodEndDate)} need to be in the same year and month.", [nameof(PeriodBeginDate), nameof(PeriodEndDate)]));
                }
            }
            return results;
        }
    }
}
