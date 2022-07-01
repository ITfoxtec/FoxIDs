using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterPlanViewModel
    {
        /// <summary>
        /// Search by plan name.
        /// </summary>
        [MaxLength(Constants.Models.Plan.NameLength)]
        [Display(Name = "Search plan")]
        public string FilterName { get; set; }
    }
}
