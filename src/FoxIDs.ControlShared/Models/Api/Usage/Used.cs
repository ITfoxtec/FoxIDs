using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class Used : UsedBase, INameValue
    {
        [Display(Name = "Tracks")]
        public decimal Tracks { get; set; }

        [Display(Name = "Users")]
        public decimal Users { get; set; }

        [Display(Name = "Logins")]
        public decimal Logins { get; set; }

        [Display(Name = "Token requests")]
        public decimal TokenRequests { get; set; }

        [Display(Name = "Control API gets")]
        public decimal ControlApiGets { get; set; }

        [Display(Name = "Control API updates")]
        public decimal ControlApiUpdates { get; set; }

        [ListLength(Constants.Models.Used.ItemsMin, Constants.Models.Used.ItemsMax)]
        public List<UsedItem> Items { get; set; }

        public string Name { get => $"{TenantName}/{PeriodBeginDate.Year}/{PeriodBeginDate.Month}"; set => _ = string.Empty; }
    }
}
