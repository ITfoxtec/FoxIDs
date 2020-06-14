using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterTenantViewModel
    {
        /// <summary>
        /// Search by tenant name.
        /// </summary>
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameRegExPattern)]
        [Display(Name = "Search tenant")]
        public string FilterName { get; set; }
    }
}
