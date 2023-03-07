using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class CreateTenantRequest : Tenant
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
        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        public string AdministratorPassword { get; set; }
        
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
    }
}
