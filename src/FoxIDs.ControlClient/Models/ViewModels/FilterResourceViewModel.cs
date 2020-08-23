using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterResourceViewModel
    {
        /// <summary>
        /// Search by user email.
        /// </summary>
        [MaxLength(Constants.Models.Resource.NameLength)]
        [Display(Name = "Search resource")]
        public string FilterName { get; set; }
    }
}
