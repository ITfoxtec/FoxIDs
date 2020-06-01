using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class SearchTenantViewModel
    {
        /// <summary>
        /// Search by tenant name.
        /// </summary>
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameRegExPattern)]
        [Display(Name = "Search tenant")]
        public string Name { get; set; }
    }
}
