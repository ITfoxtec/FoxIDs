using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Describes the type of key storage used by a track.
    /// </summary>
    public class TrackKey
    {
        /// <summary>
        /// Selected key management approach.
        /// </summary>
        [Required]
        public TrackKeyTypes Type { get; set; }
    }
}
