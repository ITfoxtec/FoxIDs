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
        public JwkWithCertificateInfo PrimaryKey { get; set; }

        public JwkWithCertificateInfo SecondaryKey { get; set; }
    }
}
