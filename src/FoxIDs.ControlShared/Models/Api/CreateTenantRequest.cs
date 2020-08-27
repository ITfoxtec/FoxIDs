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
        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [DataType(DataType.Password)]
        public string AdministratorPassword { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.MasterTrackControlClientBaseUri)]
        public string ControlClientBaseUri { get; set; }
    }
}
