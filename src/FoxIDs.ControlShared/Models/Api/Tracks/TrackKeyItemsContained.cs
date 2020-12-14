using ITfoxtec.Identity.Models;
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
        public JsonWebKey PrimaryKey { get; set; }

        public JsonWebKey SecondaryKey { get; set; }
    }
}
