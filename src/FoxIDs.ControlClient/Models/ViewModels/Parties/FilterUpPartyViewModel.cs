using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterUpPartyViewModel
    {
        /// <summary>
        /// Search by authentication method name.
        /// </summary>
        [MaxLength(Constants.Models.Party.NameLength)]
        [Display(Name = "Filter authentication methods")]
        public string FilterName { get; set; }
    }
}
