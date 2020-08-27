using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TrackKeyRequest
    {
        public bool IsPrimary { get; set; }

        [Required]
        public TrackKeyType Type { get; set; }

        [Required]
        public JsonWebKey Key { get; set; }

        public string ExternalName { get; set; }
    }
}
