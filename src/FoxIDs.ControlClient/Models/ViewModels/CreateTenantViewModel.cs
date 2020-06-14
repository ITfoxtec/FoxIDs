using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class CreateTenantViewModel
    {
        /// <summary>
        /// Tenant name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameRegExPattern)]
        [Display(Name = "Tenant name")]
        public string Name { get; set; }

        /// <summary>
        /// Administrator email.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        [Display(Name = "Administrator email")]
        public string AdministratorEmail { get; set; }

        /// <summary>
        /// Administrator password.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [DataType(DataType.Password)]
        [Display(Name = "Administrator password")]
        public string AdministratorPassword { get; set; }
    }
}
