using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class UsageRequest : IValidatableObject
    {
        [Required]
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameDbRegExPattern)]
        public string TenantName { get; set; }

        [Required]
        public DateOnly PeriodBeginDate { get; set; }

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
