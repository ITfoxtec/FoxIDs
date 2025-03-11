using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterRefreshTokenGrantViewModel
    {
        /// <summary>
        /// Search by user identifier.
        /// </summary>
        [MaxLength(Constants.Models.User.UsernameLength)]
        [Display(Name = "Search user")]
        public string FilterUserIdentifier { get; set; }

        /// <summary>
        /// Search by application client ID.
        /// </summary>
        [MaxLength(Constants.Models.Party.NameLength)]
        [Display(Name = "Application (technical name / client ID)")]
        public string FilterClientId { get; set; }

        /// <summary>
        /// Search by authentication method
        /// </summary>
        [MaxLength(Constants.Models.Party.NameLength)]
        [Display(Name = "Authentication method (technical name)")]
        public string FilterAuthMethod { get; set; }
    }
}
