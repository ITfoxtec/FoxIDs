using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterResourceViewModel
    {
        /// <summary>
        /// Search by text or ID.
        /// </summary>
        [MaxLength(Constants.Models.Resource.NameLength)]
        [Display(Name = "Search for text or ID")]
        public string FilterName { get; set; }
    }
}
