using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class CreateTenantViewModel : IValidatableObject
    {
        /// <summary>
        /// Tenant name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameRegExPattern, ErrorMessage = "The field {0} must start with a letter or a number and can contain '-' and '_'.")]
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
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        [Display(Name = "Administrator password")]
        public string AdministratorPassword { get; set; }

        /// <summary>
        /// True if the administrator account is created without an initial password and it is set with email confirmation.
        /// </summary>
        [Display(Name = "Set administrator password with email confirmation code")]
        public bool SetAdministratorPasswordEmail { get; set; }

        /// <summary>
        /// True if the administrator account password should be changed on first login. Default true.
        /// </summary>
        [Display(Name = "Change administrator password")]
        public bool ChangeAdministratorPassword { get; set; } = true;

        /// <summary>
        /// True if the administrator account should be confirmed. Default true.
        /// </summary>
        [Display(Name = "Confirm administrator account")]
        public bool ConfirmAdministratorAccount { get; set; } = true;

        [Display(Name = "Plan")]
        public string PlanName { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!SetAdministratorPasswordEmail && string.IsNullOrWhiteSpace(AdministratorPassword))
            {
                yield return new ValidationResult($"The field {nameof(AdministratorPassword)} is required.", new[] { nameof(AdministratorPassword) });
            }

            if (SetAdministratorPasswordEmail && ChangeAdministratorPassword)
            {
                yield return new ValidationResult($"The fields {nameof(SetAdministratorPasswordEmail)} and {nameof(ChangeAdministratorPassword)} can not both be true.", new[] { nameof(SetAdministratorPasswordEmail), nameof(ChangeAdministratorPassword) });
            }
        }
    }
}
