using FoxIDs.Infrastructure.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class UsedBase : IValidatableObject
    {
        [Required]
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameDbRegExPattern)]
        [Display(Name = "Tenant")]
        public string TenantName { get; set; }

        [Required]
        [Display(Name = "Period begin")]
        public DateOnly PeriodBeginDate { get; set; }

        [Required]
        [Display(Name = "Period end")]
        public DateOnly PeriodEndDate { get; set; }

        public bool IsUsageCalculated { get; set; }

        public bool IsInvoiceReady { get; set; }

        public UsagePaymentStatus PaymentStatus { get; set; }

        public bool IsDone { get; set; }

        public bool HasError { get; set; }

        [ListLength(Constants.Models.Used.InvoicesMin, Constants.Models.Used.InvoicesMax)]
        public List<Invoice> Invoices { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (PeriodBeginDate.Year != PeriodEndDate.Year || PeriodBeginDate.Month != PeriodEndDate.Month)
            {
                results.Add(new ValidationResult($"The {nameof(PeriodBeginDate)} and {nameof(PeriodEndDate)} need to be in the same year and month.", [nameof(PeriodBeginDate), nameof(PeriodEndDate)]));
            }
            return results;
        }
    }
}
