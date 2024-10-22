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
        [Min(Constants.Models.Used.YearMin)]
        public int Year { get; set; }

        [Required]
        [Range(Constants.Models.Used.MonthMin, Constants.Models.Used.MonthMax)]
        public int Month { get; set; }

        public UsedInvoiceStatus InvoiceStatus { get; set; }

        public UsedPaymentStatus PaymentStatus { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        public double TotalPrice { get; set; }
    }
}
