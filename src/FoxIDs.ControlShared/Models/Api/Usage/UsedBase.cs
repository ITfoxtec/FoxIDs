using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
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

        [Required]
        [Min(Constants.Models.Used.PeriodYearMin)]
        public int PeriodYear { get; set; }

        [Required]
        [Range(Constants.Models.Used.PeriodMonthMin, Constants.Models.Used.PeriodMonthMax)]
        public int PeriodMonth { get; set; }

        public UsedInvoiceStatus InvoiceStatus { get; set; }

        public UsedPaymentStatus PaymentStatus { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        public double TotalPrice { get; set; }
    }
}
