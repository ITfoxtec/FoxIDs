using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterTenantViewModel
    {
        /// <summary>
        /// Search by tenant name and custom domain.
        /// </summary>
        [MaxLength(Constants.Models.Tenant.CustomDomainLength)]
        [Display(Name = "Search tenant")]
        public string FilterValue { get; set; }
    }
}
