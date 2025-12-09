using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Contains primary and secondary keys stored within the track.
    /// </summary>
    public class TrackKeyItemsContained : INameValue
    {
        /// <summary>
        /// Track name the keys belong to.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameRegExPattern)]
        public string Name { get; set; }

        /// <summary>
        /// Primary signing key.
        /// </summary>
        [Required]
        public JwkWithCertificateInfo PrimaryKey { get; set; }

        /// <summary>
        /// Secondary signing key, if configured.
        /// </summary>
        public JwkWithCertificateInfo SecondaryKey { get; set; }
    }
}
