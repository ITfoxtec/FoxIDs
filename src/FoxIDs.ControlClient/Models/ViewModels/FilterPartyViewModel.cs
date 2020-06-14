using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterPartyViewModel
    {
        /// <summary>
        /// Search by party name.
        /// </summary>
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [Display(Name = "Search party")]
        public string FilterName { get; set; }
    }
}
