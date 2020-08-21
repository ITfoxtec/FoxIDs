using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TrackKeySwap
    {
        /// <summary>
        /// Track name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameRegExPattern)]
        public string TrackName { get; set; }
    }
}
