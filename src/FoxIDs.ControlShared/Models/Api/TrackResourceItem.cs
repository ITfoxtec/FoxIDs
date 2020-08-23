using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace FoxIDs.Models.Api
{
    public class TrackResourceItem : ResourceItem, INameValue
    {
        [IgnoreDataMember]
        public string Name { get => TrackName; }

        /// <summary>
        /// Track name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameRegExPattern)]
        public string TrackName { get; set; }

    }
}
