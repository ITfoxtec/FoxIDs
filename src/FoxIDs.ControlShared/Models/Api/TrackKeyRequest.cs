using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TrackKeyRequest
    {
        /// <summary>
        /// Track name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameRegExPattern)]
        public string TrackName { get; set; }

        public bool IsPrimary { get; set; }

        [Required]
        public TrackKeyType Type { get; set; }

        [Required]
        public JsonWebKey Key { get; set; }

        public string ExternalName { get; set; }
    }
}
