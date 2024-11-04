using FoxIDs.Infrastructure.DataAnnotations;
using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class UsedBase
    {
        [Required]
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameDbRegExPattern)]
        public string TenantName { get; set; }

        #region period
        [Required]
        [Min(Constants.Models.Used.PeriodYearMin)]
        public int PeriodYear { get; set; }

        [Required]
        [Range(Constants.Models.Used.PeriodMonthMin, Constants.Models.Used.PeriodMonthMax)]
        public int PeriodMonth { get; set; }

        [Required]
        public DateTime PeriodBeginDate { get; set; }

        [Required]
        public DateTime PeriodEndDate { get; set; }
        #endregion

        public bool IsUsageCalculated { get; set; }

        public bool IsInvoiceReady { get; set; }

        public UsagePaymentStatus PaymentStatus { get; set; }

        public bool IsDone { get; set; }
    }
}
