using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterUserViewModel
    {
        /// <summary>
        /// Search by user email.
        /// </summary>
        [MaxLength(Constants.Models.User.UsernameLength)]
        [Display(Name = "Search user")]
        public string FilterValue { get; set; }
    }
}
