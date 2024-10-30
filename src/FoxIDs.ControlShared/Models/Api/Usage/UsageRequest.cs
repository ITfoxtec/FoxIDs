using FoxIDs.Infrastructure.DataAnnotations;
using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class UsageRequest
    {
        [Required]
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameDbRegExPattern)]
        public string TenantName { get; set; }

        [Required]
        [Min(Constants.Models.Used.PeriodYearMin)]
        public int Year { get; set; }

        [Required]
        [Range(Constants.Models.Used.PeriodMonthMin, Constants.Models.Used.PeriodMonthMax)]
        public int Month { get; set; }
    }
}
