using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public class Used : UsedBase, INameValue
    {
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

        public string Name { get => $"{TenantName}/{PeriodBeginDate.Year}/{PeriodBeginDate.Month}"; set => _ = string.Empty; }
    }
}
