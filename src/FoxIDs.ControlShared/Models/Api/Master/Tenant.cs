using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class Tenant
    {
        [Required]
        [MaxLength(Constants.Models.TenantNameLength)]
        [RegularExpression(Constants.Models.TenantNameRegExPattern)]
        [Display(Name = "Tenant name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        [Display(Name = "Administrator email")]
        public string AdministratorEmail { get; set; }
    }
}
