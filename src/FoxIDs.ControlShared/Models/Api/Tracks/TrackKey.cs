using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TrackKey
    {
        [Required]
        public TrackKeyTypes Type { get; set; }
    }
}
