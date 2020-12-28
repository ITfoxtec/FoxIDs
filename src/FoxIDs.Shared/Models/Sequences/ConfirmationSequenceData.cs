using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class ConfirmationSequenceData : ISequenceData
    {
        [Required]
        [JsonProperty(PropertyName = "e")]
        public string Email { get; set; }
    }
}
