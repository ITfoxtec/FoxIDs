using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterFailingLoginViewModel
    {
        /// <summary>
        /// Search by user identifier.
        /// </summary>
        [MaxLength(Constants.Models.FailingLoginLock.UserIdentifierLength)]
        [Display(Name = "Search user")]
        public string FilterUserIdentifier { get; set; }
    }
}
