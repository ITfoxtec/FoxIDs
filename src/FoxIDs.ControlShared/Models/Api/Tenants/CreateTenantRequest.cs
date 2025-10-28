using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class CreateTenantRequest : Tenant, IValidatableObject
    {
        /// <summary>
        /// Administrator users email.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        [Display(Name = "Administrator users email")]
        public string AdministratorEmail { get; set; }

        /// <summary>
        /// Administrator users password.
        /// </summary>
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
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
        public bool ChangeAdministratorPassword { get; set; }

        /// <summary>
        /// True if the administrator account email should be confirmed. Default true.
        /// </summary>
        [Display(Name = "Confirm administrator account")]
        public bool ConfirmAdministratorAccount { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.MasterTrackControlClientBaseUri)]
        public string ControlClientBaseUri { get; set; }

        [ValidateComplexType]
        public Customer Customer { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!SetAdministratorPasswordEmail && string.IsNullOrWhiteSpace(AdministratorPassword))
            {
                yield return new ValidationResult($"The field {nameof(AdministratorPassword)} is required.", [nameof(AdministratorPassword)]);
            }

            if (SetAdministratorPasswordEmail && ChangeAdministratorPassword)
            {
                yield return new ValidationResult($"The fields {nameof(SetAdministratorPasswordEmail)} and {nameof(ChangeAdministratorPassword)} can not both be enabled.", [nameof(SetAdministratorPasswordEmail), nameof(ChangeAdministratorPassword)]);
            }
        }
    }
}
