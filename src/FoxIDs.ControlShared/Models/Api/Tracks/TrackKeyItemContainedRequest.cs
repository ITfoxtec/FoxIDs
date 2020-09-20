using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TrackKeyItemContainedRequest
    {
        public bool IsPrimary { get; set; }

        [Required]
        public JsonWebKey Key { get; set; }
    }
}
