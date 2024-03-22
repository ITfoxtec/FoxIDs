using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterExternalUserViewModel
    {
        /// <summary>
        /// Search by link claim or user ID.
        /// </summary>
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Search external user")]
        public string FilterValue { get; set; }
    }
}
