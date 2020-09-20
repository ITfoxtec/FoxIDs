using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TrackKeyItemsContained : INameValue
    {
        [Required]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameRegExPattern)]
        public string Name { get; set; }

        [Required]
        public TrackKeyItemContained PrimaryKey { get; set; }

        public TrackKeyItemContained SecondaryKey { get; set; }
    }
}
