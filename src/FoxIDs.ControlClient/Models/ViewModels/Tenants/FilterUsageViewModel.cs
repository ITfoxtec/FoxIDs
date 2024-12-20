﻿using FoxIDs.Infrastructure.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterUsageViewModel
    {
        [Required]
        [Min(Constants.Models.Used.PeriodYearMin)]
        [Display(Name = "Year")]
        public int PeriodYear { get; set; }

        [Required]
        [Range(Constants.Models.Used.PeriodMonthMin, Constants.Models.Used.PeriodMonthMax)]
        [Display(Name = "Month")]
        public int PeriodMonth { get; set; }

        /// <summary>
        /// Search by tenant name.
        /// </summary>
        [MaxLength(Constants.Models.Tenant.CustomDomainLength)]
        [Display(Name = "Search tenant")]
        public string FilterTenantValue { get; set; }
    }
}
