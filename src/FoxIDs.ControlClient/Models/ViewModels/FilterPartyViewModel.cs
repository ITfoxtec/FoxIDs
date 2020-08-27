using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterPartyViewModel
    {
        /// <summary>
        /// Search by party name.
        /// </summary>
        [MaxLength(Constants.Models.Party.NameLength)]
        [Display(Name = "Search party")]
        public string FilterName { get; set; }
    }
}
