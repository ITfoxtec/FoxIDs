using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterActiveSessionViewModel
    {
        [Display(Name = "User identifier (email, phone or username)")]
        public string FilterUserIdentifier { get; set; }

        [Display(Name = "Authentication method (technical name)")]
        public string FilterAuthMethod { get; set; }

        [Display(Name = "Application (technical name / client ID)")]
        public string FilterClientId { get; set; }

        [Display(Name = "Session ID")]
        public string FilterSessionId { get; set; }
    }
}
