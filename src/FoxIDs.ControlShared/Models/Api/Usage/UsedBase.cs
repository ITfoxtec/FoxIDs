using FoxIDs.Infrastructure.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Base billing model describing usage for a tenant in a given period.
    /// </summary>
    public class UsedBase : IValidatableObject
    {
        /// <summary>
        /// Name of the tenant the usage belongs to.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameDbRegExPattern)]
        [Display(Name = "Tenant")]
        public string TenantName { get; set; }

        /// <summary>
        /// Start date of the usage period.
        /// </summary>
        [Required]
        [Display(Name = "Period begin")]
        public DateOnly PeriodBeginDate { get; set; }

        /// <summary>
        /// End date of the usage period.
        /// </summary>
        [Required]
        [Display(Name = "Period end")]
        public DateOnly PeriodEndDate { get; set; }

        /// <summary>
        /// Indicates whether usage has been calculated.
        /// </summary>
        public bool IsUsageCalculated { get; set; }

        /// <summary>
        /// Indicates whether invoices are prepared for sending.
        /// </summary>
        public bool IsInvoiceReady { get; set; }

        /// <summary>
        /// Current payment status for the period.
        /// </summary>
        public UsagePaymentStatus PaymentStatus { get; set; }

        /// <summary>
        /// True if the tenant is inactive for billing.
        /// </summary>
        public bool IsInactive { get; set; }

        /// <summary>
        /// True when processing for the period is finished.
        /// </summary>
        public bool IsDone { get; set; }

        /// <summary>
        /// Indicates whether usage items have been recorded.
        /// </summary>
        public bool HasItems { get; set; }

        /// <summary>
        /// Currency code used for the invoice.
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Invoices generated for the usage period.
        /// </summary>
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
