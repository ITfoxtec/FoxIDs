using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TrackKeys : INameValue
    {
        [Required]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameRegExPattern)]
        public string Name { get; set; }

        [Required]
        public TrackKey PrimaryKey { get; set; }

        public TrackKey SecondaryKey { get; set; }
    }
}
