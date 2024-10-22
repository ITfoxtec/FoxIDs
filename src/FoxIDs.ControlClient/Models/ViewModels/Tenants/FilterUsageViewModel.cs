using FoxIDs.Infrastructure.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterUsageViewModel
    {
        [Required]
        [Min(Constants.Models.Used.YearMin)]
        public int Year { get; set; }

        [Required]
        [Range(Constants.Models.Used.MonthMin, Constants.Models.Used.MonthMax)]
        public int Month { get; set; }

        /// <summary>
        /// Search by tenant name.
        /// </summary>
        [MaxLength(Constants.Models.Tenant.CustomDomainLength)]
        [Display(Name = "Search tenant")]
        public string FilterTenantValue { get; set; }
    }
}
