using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterTrackViewModel
    {
        /// <summary>
        /// Search by environment name.
        /// </summary>
        [MaxLength(Constants.Models.Track.NameLength)]
        [Display(Name = "Filter environments")]
        public string FilterName { get; set; }
    }
}
