using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Stores Control UI preferences for a user.
    /// </summary>
    public class UserControlProfile
    {
        /// <summary>
        /// Name of the last track accessed.
        /// </summary>
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameDbRegExPattern)]
        [Display(Name = "Last environment")]
        public string LastTrackName { get; set; }        
    }
}
