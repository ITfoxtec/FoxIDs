using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TrackKeyItemContained
    {
        [Required]
        public JsonWebKey Key { get; set; }
    }
}
