using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterUpPartyViewModel
    {
        /// <summary>
        /// Search by party name.
        /// </summary>
        [MaxLength(Constants.Models.Party.NameLength)]
        [Display(Name = "Search up-party")]
        public string FilterName { get; set; }
    }
}
