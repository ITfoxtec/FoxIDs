using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterDownPartyViewModel
    {
        /// <summary>
        /// Search by application name.
        /// </summary>
        [MaxLength(Constants.Models.Party.NameLength)]
        [Display(Name = "Filter applications")]
        public string FilterName { get; set; }
    }
}
