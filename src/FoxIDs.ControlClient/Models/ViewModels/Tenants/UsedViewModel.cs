using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class UsedViewModel 
    {
        public UsedViewModel()
        {
            Items = new List<UsedItem>();
        }

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

        public bool IsUsageCalculated { get; set; }

        public bool IsInvoiceReady { get; set; }

        public UsagePaymentStatus PaymentStatus { get; set; }

        public bool IsDone { get; set; }

        public decimal Tracks { get; set; }

        public decimal Users { get; set; }

        public decimal Logins { get; set; }

        public decimal TokenRequests { get; set; }

        public decimal ControlApiGets { get; set; }

        public decimal ControlApiUpdates { get; set; }

        [ListLength(Constants.Models.Used.ItemsMin, Constants.Models.Used.ItemsMax)]
        public List<UsedItem> Items { get; set; }

        [ListLength(Constants.Models.Used.InvoicesMin, Constants.Models.Used.InvoicesMax)]
        public List<Invoice> Invoices { get; set; }

    }
}
