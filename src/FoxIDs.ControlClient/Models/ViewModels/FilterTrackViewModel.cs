using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FilterTrackViewModel
    {
        /// <summary>
        /// Search by track name.
        /// </summary>
        [MaxLength(Constants.Models.Track.NameLength)]
        [Display(Name = "Search track")]
        public string FilterName { get; set; }
    }
}
