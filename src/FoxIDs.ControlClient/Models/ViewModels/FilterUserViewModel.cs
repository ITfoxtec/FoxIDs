using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterUserViewModel
    {
        /// <summary>
        /// Search by user email.
        /// </summary>
        [MaxLength(Constants.Models.User.EmailLength)]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        [Display(Name = "Search user")]
        public string FilterEmail { get; set; }
    }
}
