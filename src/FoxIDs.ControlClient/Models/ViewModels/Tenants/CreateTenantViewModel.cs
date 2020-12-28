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
        [RegularExpression(Constants.Models.Tenant.NameRegExPattern, ErrorMessage = "The field {0} must start with a letter or number and can contain '-' and '_'.")]
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
        /// True if the administrator account should be confirmed.
        /// </summary>
        [Display(Name = "Confirm administrator account")]
        public bool ConfirmAdministratorAccount { get; set; }

        /// <summary>
        /// Administrator password.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        [Display(Name = "Administrator password")]
        public string AdministratorPassword { get; set; }
    }
}
