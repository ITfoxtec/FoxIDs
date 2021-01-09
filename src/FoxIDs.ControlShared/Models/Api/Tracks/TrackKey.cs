using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TrackKey
    {
        [Required]
        public TrackKeyType Type { get; set; }
    }
}
