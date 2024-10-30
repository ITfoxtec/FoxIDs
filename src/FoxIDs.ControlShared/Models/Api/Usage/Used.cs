using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public class Used : UsedBase, INameValue
    {
        public double Tracks { get; set; }

        public double Users { get; set; }

        public double Logins { get; set; }

        public double TokenRequests { get; set; }

        public double ControlApiGets { get; set; }

        public double ControlApiUpdates { get; set; }

        [ListLength(Constants.Models.Used.ItemsMin, Constants.Models.Used.ItemsMax)]
        public List<UsedItem> Items { get; set; }

        [ListLength(Constants.Models.Used.InvoicesMin, Constants.Models.Used.InvoicesMax)]
        public List<Invoice> Invoices { get; set; }

        public string Name { get => $"{TenantName}/{PeriodYear}/{PeriodMonth}"; set => _ = string.Empty; }
    }
}
